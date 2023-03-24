using System.Diagnostics;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Orleans.Storage;

/// <summary>
///     EventStore-based grain storage provider
/// </summary>
public class EventStoreGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
{
    private readonly string _name;
    private readonly EventStoreStorageOptions _options;
    private readonly IGrainStorageSerializer _storageSerializer;
    private readonly ILogger<EventStoreGrainStorage> _logger;
    private readonly string _serviceId;

    private EventStoreClient? _client;
    private bool _initialized;

    /// <summary>
    ///     Creates a new instance of the <see cref="EventStoreGrainStorage" /> type.
    /// </summary>
    public EventStoreGrainStorage(string name, EventStoreStorageOptions options, IOptions<ClusterOptions> clusterOptions, ILogger<EventStoreGrainStorage> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(options.GrainStorageSerializer, nameof(options.GrainStorageSerializer));
        ArgumentNullException.ThrowIfNull(clusterOptions, nameof(clusterOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _name = name;
        _options = options;
        _storageSerializer = options.GrainStorageSerializer;
        _serviceId = clusterOptions.Value.ServiceId;
        _logger = logger;
    }

    /// <summary>
    /// </summary>
    /// <param name="grainId"></param>
    /// <returns></returns>
    private string GetStreamName(GrainId grainId)
    {
        return $"{_serviceId}/{_name}/state/{grainId}";
    }

    #region Lifecycle Participant

    /// <inheritdoc />
    public void Participate(ISiloLifecycle lifecycle)
    {
        var name = OptionFormattingUtilities.Name<EventStoreGrainStorage>(_name);
        lifecycle.Subscribe(name, _options.InitStage, Init, Close);
    }

    private Task Init(CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreGrainStorage {Name} is initializing: ServiceId={ServiceId}", _name, _serviceId);
            }
            _client = new EventStoreClient(_options.ClientSettings);
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
        finally
        {
            _client = null;
            _initialized = false;
        }
    }

    #endregion

    #region Storage

    /// <inheritdoc />
    public async Task ReadStateAsync<T>(string grainTypeName, GrainId grainId, IGrainState<T> grainState)
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        var streamName = GetStreamName(grainId);
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1, false, null, _options.Credentials);
            var readState = await readResult.ReadState.ConfigureAwait(false);
            if (readState == ReadState.Ok)
            {
                var resolvedEvent = await readResult.FirstOrDefaultAsync().ConfigureAwait(false);
                var deserializeState = DeserializeState<T>(resolvedEvent);
                grainState.State = deserializeState.State;
                grainState.ETag = deserializeState.ETag;
                grainState.RecordExists = true;
            }
            else
            {
                grainState.ETag = null;
                grainState.RecordExists = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read grain state for {GrainType} grain with ID {GrainId} and stream key {Key}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read grain state for {grainTypeName} with ID {grainId} and stream key {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task WriteStateAsync<T>(string grainTypeName, GrainId grainId, IGrainState<T> grainState)
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        var streamName = GetStreamName(grainId);
        try
        {
            var eTagExists = ulong.TryParse(grainState.ETag, out var eTag);
            var serializedState = SerializeState(grainState.State);
            var writeResult = await _client.AppendToStreamAsync(streamName, eTagExists ? new StreamRevision(eTag) : StreamRevision.None, new[] { serializedState }, null, null, _options.Credentials).ConfigureAwait(false);
            grainState.ETag = writeResult.NextExpectedStreamRevision.ToUInt64().ToString();
            grainState.RecordExists = true;
        }
        catch (WrongExpectedVersionException)
        {
            _logger.LogWarning("Version conflict for {GrainType} grain with ID {GrainId} and stream key {Key} on WriteStateAsync.", grainTypeName, grainId, streamName);
            throw new InconsistentStateException($"Version conflict ({nameof(WriteStateAsync)}): ServiceId={_serviceId} ProviderName={_name} GrainType={grainTypeName} GrainId={grainId} ETag={grainState.ETag}.");
        }
        catch (Exception ex) when (ex is not InconsistentStateException)
        {
            _logger.LogError("Failed to write grain state for {GrainType} grain with ID {GrainId} and stream key {Key}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write grain state for {grainTypeName} with ID {grainId} and stream key {streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task ClearStateAsync<T>(string grainTypeName, GrainId grainId, IGrainState<T> grainState)
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        var streamName = GetStreamName(grainId);
        try
        {
            var eTagExists = ulong.TryParse(grainState.ETag, out var eTag);
            if (_options.DeleteStateOnClear)
            {
                if (eTagExists)
                {
                    await _client.DeleteAsync(streamName, new StreamRevision(eTag), null, _options.Credentials).ConfigureAwait(false);
                    grainState.ETag = eTag.ToString();
                }
            }
            else
            {
                if (eTagExists)
                {
                    var serializedState = SerializeState(default(T));
                    var writeResult = await _client.AppendToStreamAsync(streamName, new StreamRevision(eTag), new[] { serializedState }, null, null, _options.Credentials).ConfigureAwait(false);
                    grainState.ETag = writeResult.NextExpectedStreamRevision.ToUInt64().ToString();
                }
            }
            grainState.RecordExists = false;
        }
        catch (WrongExpectedVersionException)
        {
            _logger.LogWarning("Version conflict for {GrainType} grain with ID {GrainId} and stream key {Key} on ClearStateAsync.", grainTypeName, grainId, streamName);
            throw new InconsistentStateException($"Version conflict ({nameof(ClearStateAsync)}): ServiceId={_serviceId} ProviderName={_name} GrainType={grainTypeName} GrainId={grainId} ETag={grainState.ETag}.");
        }
        catch (Exception ex) when (ex is not InconsistentStateException)
        {
            _logger.LogError("Failed to clear grain state for {GrainType} grain with ID {GrainId} and stream key {Key}.", grainTypeName, grainId, streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to clear grain state for grain {grainTypeName} with ID {grainId}. {ex.GetType()}: {ex.Message}"));
        }
    }

    #endregion

    #region Serialize & Deserialize

    /// <summary>
    /// </summary>
    /// <param name="state"></param>
    /// <typeparam name="TState"></typeparam>
    /// <returns></returns>
    private EventData SerializeState<TState>(TState state)
    {
        var contentType = _storageSerializer is JsonGrainStorageSerializer ? "application/json" : "application/octet-stream";
        if (state is null)
        {
            return new EventData(Uuid.NewUuid(), typeof(TState).Name, new ReadOnlyMemory<byte>(), null, contentType);
        }
        var stateData = _storageSerializer.Serialize(state);
        return new EventData(Uuid.NewUuid(), state.GetType().Name, stateData.ToMemory(), null, contentType);
    }

    /// <summary>
    /// </summary>
    /// <param name="evt"></param>
    /// <typeparam name="TState"></typeparam>
    /// <returns></returns>
    private (TState State, string ETag) DeserializeState<TState>(ResolvedEvent evt)
    {
        var state = _storageSerializer.Deserialize<TState>(evt.Event.Data) ?? Activator.CreateInstance<TState>();
        return (state, evt.Event.EventNumber.ToUInt64().ToString());
    }

    #endregion

}
