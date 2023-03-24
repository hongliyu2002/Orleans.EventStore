using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.Streaming.EventStoreStorage;

/// <summary>
///     Utility class to encapsulate data access to EventStore storage.
/// </summary>
public class EventStoreStateManager : IDisposable, IAsyncDisposable
{
    private readonly EventStoreOperationOptions _options;
    private readonly EventStorePolicyOptions _policyOptions;
    private readonly Serializer _serializer;
    private readonly ILogger _logger;

    private EventStoreClient? _client;
    private bool _initialized;

    /// <summary>
    ///     Creates a new <see cref="EventStoreStateManager" /> instance.
    /// </summary>
    /// <param name="options">Storage configuration.</param>
    /// <param name="serializer"></param>
    /// <param name="logger">Logger to use.</param>
    public EventStoreStateManager(EventStoreOperationOptions options, Serializer serializer, ILogger<EventStoreStateManager> logger)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(options.PolicyOptions, nameof(options.PolicyOptions));
        ArgumentNullException.ThrowIfNull(serializer, nameof(serializer));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _options = options;
        _serializer = serializer;
        _policyOptions = options.PolicyOptions;
        _serializer = serializer;
        _logger = logger;
    }

    #region Lifecycle

    /// <summary>
    ///     Creates the EventStore client.
    /// </summary>
    public void Init()
    {
        const string operation = "Init";
        var startTime = DateTime.UtcNow;
        try
        {
            _client = new EventStoreClient(_options.ClientSettings);
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(EventStoreErrorCodes.FailCreatingClient, ex, "Failed to init EventStore client.");
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to init EventStore client, {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            CheckAlertSlowAccess(startTime, operation);
        }
    }

    /// <summary>
    ///     Disposes the EventStore client.
    /// </summary>
    public async Task Close()
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        const string operation = "Close";
        var startTime = DateTime.UtcNow;
        try
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventStoreErrorCodes.FailDisposingClient, ex, "Failed to close EventStore client.");
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to init EventStore client, {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _client = null;
            _initialized = false;
            CheckAlertSlowAccess(startTime, operation);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        const string operation = "Close";
        var startTime = DateTime.UtcNow;
        try
        {
            _client.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(EventStoreErrorCodes.FailDisposingClient, ex, "Failed to close EventStore client.");
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to init EventStore client, {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _client = null;
            _initialized = false;
            CheckAlertSlowAccess(startTime, operation);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Close().ConfigureAwait(false);
    }

    #endregion

    #region Storage

    /// <summary>
    ///     Reads a state entry from the EventStore stream.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    public async Task<T?> ReadStateAsync<T>(string streamName)
        where T : class, IEventStoreState, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName, nameof(streamName));
        if (_initialized == false || _client == null)
        {
            _logger.LogWarning(EventStoreErrorCodes.CannotInitializeClient, "EventStore client for stream {StreamName} is not initialized.", streamName);
            throw new InvalidOperationException(FormattableString.Invariant($"EventStore client for stream {streamName} is not initialized."));
        }
        const string operation = "ReadState";
        var startTime = DateTime.UtcNow;
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("{Operation} entry {State} of stream {StreamName}", operation, typeof(T), streamName);
        }
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1, false, null, _options.Credentials);
            var readState = await readResult.ReadState.ConfigureAwait(false);
            if (readState == ReadState.Ok)
            {
                var resolvedEvent = await readResult.FirstOrDefaultAsync().ConfigureAwait(false);
                var deserializeState = DeserializeState<T>(resolvedEvent);
                var state = deserializeState.State;
                state.ETag = deserializeState.ETag;
                return state;
            }
            else
            {
                _logger.LogWarning(EventStoreErrorCodes.CannotReadStateFromStream, "Failed to read state for stream {StreamName}.", streamName);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(EventStoreErrorCodes.CannotReadStateFromStream, "Failed to read state for stream {StreamName}.", streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read state for stream {streamName}. {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            CheckAlertSlowAccess(startTime, operation, streamName);
        }
    }

    /// <summary>
    ///     Append a state entry to the EventStore stream.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    /// <param name="state">State to be appended to the stream.</param>
    /// <param name="ignoreETag">If ETag should be checked.</param>
    public async Task<T> WriteStateAsync<T>(string streamName, T state, bool ignoreETag = false)
        where T : class, IEventStoreState, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName, nameof(streamName));
        if (_initialized == false || _client == null)
        {
            _logger.LogWarning(EventStoreErrorCodes.CannotInitializeClient, "EventStore client for stream {StreamName} is not initialized.", streamName);
            throw new InvalidOperationException(FormattableString.Invariant($"EventStore client for stream {streamName} is not initialized."));
        }
        const string operation = "WriteState";
        var startTime = DateTime.UtcNow;
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("{Operation} entry {State} of stream {StreamName}", operation, state, streamName);
        }
        try
        {
            var eTagExists = ulong.TryParse(state.ETag, out var eTag);
            var serializedState = SerializeState(state);
            IWriteResult writeResult;
            if (ignoreETag)
            {
                writeResult = await _client.AppendToStreamAsync(streamName, StreamState.Any, new[] { serializedState }, null, null, _options.Credentials).ConfigureAwait(false);
            }
            else
            {
                writeResult = await _client.AppendToStreamAsync(streamName, eTagExists ? new StreamRevision(eTag) : StreamRevision.None, new[] { serializedState }, null, null, _options.Credentials).ConfigureAwait(false);
            }
            state.ETag = writeResult.NextExpectedStreamRevision.ToUInt64().ToString();
            return state;
        }
        catch (WrongExpectedVersionException)
        {
            _logger.LogWarning(EventStoreErrorCodes.VersionConflictInStream, "Version conflict for stream {StreamName} when write state.", streamName);
            throw new InconsistentStateException($"Version conflict when write state: ETag={state.ETag}.");
        }
        catch (Exception ex) when (ex is not InconsistentStateException)
        {
            _logger.LogError(EventStoreErrorCodes.CannotWriteStateToStream, "Failed to write state for stream {StreamName}.", streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write state for stream {streamName}. {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            CheckAlertSlowAccess(startTime, operation, streamName);
        }
    }

    /// <summary>
    ///     Clear a state entry of the EventStore stream.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    /// <param name="state">State to be cleared of the stream.</param>
    /// <param name="ignoreETag">If ETag should be checked.</param>
    public async Task ClearStateAsync<T>(string streamName, T state, bool ignoreETag)
        where T : class, IEventStoreState, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName, nameof(streamName));
        if (_initialized == false || _client == null)
        {
            _logger.LogWarning(EventStoreErrorCodes.CannotInitializeClient, "EventStore client for stream {StreamName} is not initialized.", streamName);
            throw new InvalidOperationException(FormattableString.Invariant($"EventStore client for stream {streamName} is not initialized."));
        }
        const string operation = "ClearState";
        var startTime = DateTime.UtcNow;
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("{Operation} entry {State} of stream {StreamName}", operation, state, streamName);
        }
        try
        {
            var eTagExists = ulong.TryParse(state.ETag, out var eTag);
            if (!ignoreETag && eTagExists)
            {
                await _client.DeleteAsync(streamName, new StreamRevision(eTag), null, _options.Credentials).ConfigureAwait(false);
                return;
            }
            await _client.DeleteAsync(streamName, StreamState.Any, null, _options.Credentials).ConfigureAwait(false);
        }
        catch (WrongExpectedVersionException)
        {
            _logger.LogWarning(EventStoreErrorCodes.VersionConflictInStream, "Version conflict for stream {StreamName} when clear state.", streamName);
            throw new InconsistentStateException($"Version conflict when clear state: ETag={state.ETag}.");
        }
        catch (Exception ex) when (ex is not InconsistentStateException)
        {
            _logger.LogError(EventStoreErrorCodes.CannotClearStateToStream, "Failed to clear state for stream {StreamName}.", streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to clear state for stream {streamName}. {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            CheckAlertSlowAccess(startTime, operation, streamName);
        }
    }

    #endregion

    #region Helper

    private void CheckAlertSlowAccess(DateTime startOperation, string operation, string? streamName = null)
    {
        var timeSpan = DateTime.UtcNow - startOperation;
        if (timeSpan <= _policyOptions.OperationTimeout)
        {
            return;
        }
        if (streamName == null)
        {
            _logger.LogWarning(EventStoreErrorCodes.SlowAccessToStream, "Slow access to EventStore for {Operation}, which took {Duration}", operation, timeSpan);
        }
        else
        {
            _logger.LogWarning(EventStoreErrorCodes.SlowAccessToStream, "Slow access to EventStore stream {StreamName} for {Operation}, which took {Duration}", streamName, operation, timeSpan);
        }
    }

    #endregion

    #region Serialize & Deserialize

    /// <summary>
    ///     Serializes the provided event store state object into an event data instance.
    /// </summary>
    /// <typeparam name="T">The type of the event store state object being serialized.</typeparam>
    /// <param name="state">The event store state object being serialized.</param>
    /// <returns>An event data instance representing the serialized state object.</returns>
    private EventData SerializeState<T>(T state)
        where T : class, IEventStoreState, new()
    {
        if (state is null)
        {
            return new EventData(Uuid.NewUuid(), typeof(T).Name, new ReadOnlyMemory<byte>(), null, "application/octet-stream");
        }
        var stateBuffer = _serializer.SerializeToArray(state);
        return new EventData(Uuid.NewUuid(), state.GetType().Name, new ReadOnlyMemory<byte>(stateBuffer), null, "application/octet-stream");
    }

    /// <summary>
    ///     Deserializes the provided event data into an event store state object.
    /// </summary>
    /// <typeparam name="T">The type of the event store state object being deserialized.</typeparam>
    /// <param name="evt">The resolved event containing the serialized state data.</param>
    /// <returns>A tuple containing the deserialized state object and its ETag.</returns>
    private (T State, string ETag) DeserializeState<T>(ResolvedEvent evt)
        where T : class, IEventStoreState, new()
    {
        var state = _serializer.Deserialize<T>(evt.Event.Data) ?? Activator.CreateInstance<T>();
        return (state, evt.Event.EventNumber.ToUInt64().ToString());
    }

    #endregion

}
