using System.Diagnostics;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     EventStore-based log consistent storage provider
/// </summary>
public class EventStoreLogConsistentStorage : ILogConsistentStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _name;
    private readonly EventStoreStorageOptions _storageOptions;
    private readonly IGrainStorageSerializer _storageSerializer;
    private readonly ILogger<EventStoreLogConsistentStorage> _logger;
    private readonly string _serviceId;

    private EventStoreClient? _client;

    private bool _initialized;

    /// <summary>
    ///     Creates a new instance of the <see cref="EventStoreLogConsistentStorage" /> type.
    /// </summary>
    public EventStoreLogConsistentStorage(string name, EventStoreStorageOptions storageOptions, IOptions<ClusterOptions> clusterOptions, ILogger<EventStoreLogConsistentStorage> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(storageOptions, nameof(storageOptions));
        ArgumentNullException.ThrowIfNull(clusterOptions, nameof(clusterOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _name = name;
        _storageOptions = storageOptions;
        _storageSerializer = storageOptions.GrainStorageSerializer;
        _serviceId = clusterOptions.Value.ServiceId;
        _logger = logger;
    }

    /// <summary>
    /// </summary>
    /// <param name="grainId"></param>
    /// <returns></returns>
    private string GetStreamName(GrainId grainId)
    {
        return $"{_serviceId}/log/{grainId}";
    }

    #region Lifecycle Participant

    /// <inheritdoc />
    public void Participate(ISiloLifecycle lifecycle)
    {
        var name = OptionFormattingUtilities.Name<EventStoreLogConsistentStorage>(_name);
        lifecycle.Subscribe(name, _storageOptions.InitStage, Init, Close);
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
            _client = new EventStoreClient(_storageOptions.ClientSettings);
            _initialized = true;
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

    private async Task Close(CancellationToken cancellationToken)
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        try
        {
            await _client.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Close: Name={Name} ServiceId={ServiceId}", _name, _serviceId);
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
    }

    #endregion

    #region Storage

    /// <inheritdoc />
    public async Task<IReadOnlyList<TLogEntry>> ReadAsync<TLogEntry>(string grainTypeName, GrainId grainId, int fromVersion, int maxCount)
    {
        if (_initialized == false || _client == null || maxCount <= 0)
        {
            return new List<TLogEntry>();
        }
        var streamName = GetStreamName(grainId);
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.FromInt64(fromVersion), maxCount, false, null, _storageOptions.Credentials);
            var readState = await readResult.ReadState.ConfigureAwait(false);
            if (readState == ReadState.StreamNotFound)
            {
                return new List<TLogEntry>();
            }
            return await readResult.Select(DeserializeEvent<TLogEntry>).OrderBy(x => x.Version).Select(x => x.LogEntry).ToListAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read log entries for {GrainType} grain with ID {GrainId} and stream {StreamName}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read log entries for {grainTypeName} with ID {grainId} and stream {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<int> GetLastVersionAsync(string grainTypeName, GrainId grainId)
    {
        if (_initialized == false || _client == null)
        {
            return -1;
        }
        var streamName = GetStreamName(grainId);
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1, false, null, _storageOptions.Credentials);
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
            _logger.LogError("Failed to read last log entry for {GrainType} grain with ID {GrainId} and stream {StreamName}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read last log entry for {grainTypeName} with ID {grainId} and stream {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<int> AppendAsync<TLogEntry>(string grainTypeName, GrainId grainId, IList<TLogEntry> entries, int expectedVersion)
    {
        if (_initialized == false || _client == null)
        {
            return -1;
        }
        var streamName = GetStreamName(grainId);
        if (entries.Count == 0)
        {
            return await GetLastVersionAsync(grainTypeName, grainId);
        }
        try
        {
            var serializedEntries = entries.Select(SerializeEvent);
            var writeResult = await _client.AppendToStreamAsync(streamName, new StreamRevision((ulong)expectedVersion), serializedEntries, null, null, _storageOptions.Credentials).ConfigureAwait(false);
            return (int)writeResult.NextExpectedStreamRevision.ToUInt64();
        }
        catch (WrongExpectedVersionException)
        {
            throw new InconsistentStateException($"Version conflict ({nameof(AppendAsync)}): ServiceId={_serviceId} ProviderName={_name} GrainType={grainTypeName} GrainId={grainId} Version={expectedVersion}.");
        }
        catch (Exception ex) when (ex is not InconsistentStateException)
        {
            _logger.LogError("Failed to write log entries for {GrainType} grain with ID {GrainId} and stream {StreamName}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write log entries for {grainTypeName} with ID {grainId} and stream {streamName}. {ex.GetType()}: {ex.Message}"));
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
        var contentType = _storageSerializer is JsonGrainStorageSerializer ? "application/json" : "application/octet-stream";
        if (entry is null)
        {
            return new EventData(Uuid.NewUuid(), typeof(TLogEntry).Name, new ReadOnlyMemory<byte>(), null, contentType);
        }
        var entryData = _storageSerializer.Serialize(entry);
        return new EventData(Uuid.NewUuid(), entry.GetType().Name, entryData.ToMemory(), null, contentType);
    }

    /// <summary>
    /// </summary>
    /// <param name="evt"></param>
    /// <typeparam name="TLogEntry"></typeparam>
    /// <returns></returns>
    private (TLogEntry LogEntry, int Version) DeserializeEvent<TLogEntry>(ResolvedEvent evt)
    {
        var entry = _storageSerializer.Deserialize<TLogEntry>(evt.Event.Data) ?? Activator.CreateInstance<TLogEntry>();
        return (entry, (int)evt.Event.EventNumber.ToUInt64());
    }

    #endregion

}
