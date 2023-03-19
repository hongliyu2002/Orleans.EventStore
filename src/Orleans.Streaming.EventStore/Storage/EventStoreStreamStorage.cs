using System.Diagnostics;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Storage;
using Orleans.Streaming.Configuration;

namespace Orleans.Streaming.EventStoreStorage;

/// <summary>
///     EventStore-based stream storage provider
/// </summary>
public class EventStoreStreamStorage
{
    private readonly string _streamName;
    private readonly EventStoreStorageOptions _storageOptions;
    private readonly ILogger<EventStoreStreamStorage> _logger;

    private EventStoreClient _client = null!;

    /// <summary>
    ///     Creates a new instance of the <see cref="EventStoreStreamStorage" /> type.
    /// </summary>
    public EventStoreStreamStorage(string streamName, EventStoreStorageOptions storageOptions, ILogger<EventStoreStreamStorage> logger)
    {
        _streamName = streamName;
        _storageOptions = storageOptions;
        ArgumentException.ThrowIfNullOrEmpty(streamName, nameof(streamName));
        ArgumentNullException.ThrowIfNull(storageOptions, nameof(storageOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _streamName = streamName;
        _storageOptions = storageOptions;
        _logger = logger;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    private string GetStreamName()
    {
        return $"streaming/{_streamName}";
    }

    #region Lifecycle

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EventStoreStorageException"></exception>
    public Task Init()
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreStreamStorage for stream {StreamName} is initializing.", _streamName);
            }
            _client = new EventStoreClient(_storageOptions.ClientSettings);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                timer.Stop();
                _logger.LogDebug("Init: StreamName={StreamName}, initialized in {ElapsedMilliseconds} ms", _streamName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Init: StreamName={StreamName}, errored in {ElapsedMilliseconds} ms.", _streamName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public Task Close()
    {
        return _client.DisposeAsync().AsTask();
    }

    #endregion

    #region Storage

    /// <summary>
    /// </summary>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    /// <exception cref="EventStoreStorageException"></exception>
    public async Task<IReadOnlyList<EventRecord>> ReadLastAsync(int maxCount)
    {
        if (maxCount <= 0)
        {
            return new List<EventRecord>();
        }
        var streamName = GetStreamName();
        try
        {
            var readResult = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, maxCount);
            var readState = await readResult.ReadState.ConfigureAwait(false);
            if (readState == ReadState.StreamNotFound)
            {
                return new List<EventRecord>();
            }
            return await readResult.Select(x => x.Event).OrderBy(x => x.EventNumber).ToListAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read events for the stream {StreamName}.", _streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read events for the stream {_streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EventStoreStorageException"></exception>
    public async Task<int> GetCountAsync()
    {
        var streamName = GetStreamName();
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
            _logger.LogError("Failed to read count for the stream {StreamName}.", _streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to read count for the stream {_streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="events"></param>
    /// <returns></returns>
    public async Task<int> AppendAsync(IEnumerable<EventData> events)
    {
        var streamName = GetStreamName();
        var eventsList = events as EventData[] ?? events.ToArray();
        if (!eventsList.Any())
        {
            return await GetCountAsync();
        }
        try
        {
            var writeResult = await _client.AppendToStreamAsync(streamName, StreamState.Any, eventsList).ConfigureAwait(false);
            return (int)writeResult.NextExpectedStreamRevision.ToUInt64();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write events for the stream {StreamName}.", _streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write events for the stream {_streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InconsistentStateException"></exception>
    /// <exception cref="EventStoreStorageException"></exception>
    public async Task DeleteAsync()
    {
        var streamName = GetStreamName();
        try
        {
            await _client.DeleteAsync(streamName, StreamState.Any).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete the stream {StreamName}.", _streamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to delete the stream {_streamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    #endregion

}
