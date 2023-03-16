using System.Diagnostics;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.EventSourcing.Configuration;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     EventStore-based gog consistent storage provider
/// </summary>
public class EventStoreLogConsistentStorage : ILogConsistentStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _name;
    private readonly EventStoreStorageOptions _options;
    private readonly IGrainStorageSerializer _storageSerializer;
    private readonly ILogger<EventStoreLogConsistentStorage> _logger;
    private readonly string _serviceId;

    private EventStoreClient _client = null!;

    /// <summary>
    ///     Creates a new instance of the <see cref="EventStoreLogConsistentStorage" /> type.
    /// </summary>
    public EventStoreLogConsistentStorage(string name, EventStoreStorageOptions storageOptions, IOptions<ClusterOptions> clusterOptions, ILogger<EventStoreLogConsistentStorage> logger)
    {
        _name = name;
        _options = storageOptions;
        _storageSerializer = storageOptions.GrainStorageSerializer;
        _serviceId = clusterOptions.Value.ServiceId;
        _logger = logger;
    }

    /// <summary>
    /// </summary>
    /// <param name="grainTypeName"></param>
    /// <param name="grainId"></param>
    /// <returns></returns>
    private string GetStreamName(string grainTypeName, GrainId grainId)
    {
        return $"{_serviceId}-{grainTypeName}-{grainId.Key}";
    }

    #region Lifecycle Participant

    /// <inheritdoc />
    public void Participate(ISiloLifecycle lifecycle)
    {
        var name = OptionFormattingUtilities.Name<EventStoreLogConsistentStorage>(_name);
        lifecycle.Subscribe(name, _options.InitStage, Init, Close);
    }

    private Task Init(CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreLogConsistentStorage {Name} is initializing: ServiceId={ServiceId}", _name, _serviceId);
            }
            _client = new EventStoreClient(_options.ClientSettings);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                timer.Stop();
                _logger.LogDebug("Init: Name={Name} ServiceId={ServiceId}, initialized in {ElapsedMilliseconds} ms", _name, _serviceId, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Init: Name={Name} ServiceId={ServiceId}, errored in {ElapsedMilliseconds} ms.", _name, _serviceId, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
        return Task.CompletedTask;
    }

    private Task Close(CancellationToken cancellationToken)
    {
        return _client.DisposeAsync().AsTask();
    }

    #endregion

    #region Storage

    /// <inheritdoc />
    public async Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IEnumerable<TLogEntry> entries, int expectedVersion)
    {
        var logEntries = entries as TLogEntry[] ?? entries.ToArray();
        if (!logEntries.Any())
        {
            return expectedVersion;
        }
        var streamName = GetStreamName(grainTypeName, grainId);
        try
        {
            var eventData = logEntries.Select(SerializeEvent);
            var writeResult = await _client.AppendToStreamAsync(streamName, new StreamRevision((ulong)expectedVersion), eventData).ConfigureAwait(false);
            return (int)writeResult.NextExpectedStreamRevision.ToUInt64();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write log entries for {GrainType} grain with ID {GrainId} and stream key {Key}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write log entries for {grainTypeName} with ID {grainId} and stream key {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion, int length)
    {
        if (length <= 0)
        {
            return new List<TLogEntry>();
        }
        var streamName = GetStreamName(grainTypeName, grainId);
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.FromInt64(fromVersion), length);
            var readState = await readResult.ReadState.ConfigureAwait(false);
            if (readState == ReadState.StreamNotFound)
            {
                return new List<TLogEntry>();
            }
            return await readResult.Select(DeserializeEvent<TLogEntry>).OrderBy(x => x.Version).Select(x => x.LogEntry).ToListAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read log entries for {GrainType} grain with ID {GrainId} and stream key {Key}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read log entries for {grainTypeName} with ID {grainId} and stream key {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId)
    {
        var streamName = GetStreamName(grainTypeName, grainId);
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1);
            var readState = await readResult.ReadState.ConfigureAwait(false);
            if (readState == ReadState.StreamNotFound)
            {
                return -1;
            }
            var resolvedEvent = await readResult.FirstOrDefaultAsync().ConfigureAwait(false);
            return (int)resolvedEvent.Event.EventNumber.ToUInt64();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read last log entry for {GrainType} grain with ID {GrainId} and stream key {Key}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read last log entry for {grainTypeName} with ID {grainId} and stream key {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    #endregion

    #region Serialize & Deserialize

    /// <summary>
    /// </summary>
    /// <param name="entry"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    private EventData SerializeEvent<TLogEntry>(TLogEntry entry)
    {
        if (entry is null)
        {
            return new EventData(Uuid.NewUuid(), typeof(TLogEntry).Name, new ReadOnlyMemory<byte>());
        }
        // var eventJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entry, _jsonDefaultSettings));
        var eventJson = _storageSerializer.Serialize(entry);
        return new EventData(Uuid.NewUuid(), entry.GetType().Name, eventJson.ToMemory());
    }

    /// <summary>
    /// </summary>
    /// <param name="evt"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    private (TLogEntry LogEntry, int Version) DeserializeEvent<TLogEntry>(ResolvedEvent evt)
    {
        // var eventJson = Encoding.UTF8.GetString(evt.Event.Data.ToArray());
        // var logEntry = JsonConvert.DeserializeObject<TLogEntry>(eventJson, _jsonDefaultSettings) ?? Activator.CreateInstance<TLogEntry>();
        var logEntry = _storageSerializer.Deserialize<TLogEntry>(evt.Event.Data) ?? Activator.CreateInstance<TLogEntry>();
        return (logEntry, (int)evt.Event.EventNumber.ToUInt64());
    }

    #endregion

}
