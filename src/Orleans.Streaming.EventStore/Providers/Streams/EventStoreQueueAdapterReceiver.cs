using System.Diagnostics;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Statistics;
using Orleans.Streaming.EventStoreStorage;
using Orleans.Streams;
using StreamPosition = Orleans.Streams.StreamPosition;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Receives batches of messages from a single queue of a message queue.
/// </summary>
internal class EventStoreQueueAdapterReceiver : IQueueAdapterReceiver, IQueueCache
{
    public const int MaxMessagesPerRead = 1000;

    private readonly EventStoreReceiverSettings _settings;
    private readonly Func<string, IStreamQueueCheckpointer<string>, ILoggerFactory, IEventStoreQueueCache> _cacheFactory;
    private readonly Func<string, Task<IStreamQueueCheckpointer<string>>> _checkpointerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<EventStoreQueueAdapterReceiver> _logger;
    private readonly IQueueAdapterReceiverMonitor _receiverMonitor;
    private readonly LoadSheddingOptions _loadSheddingOptions;
    private readonly IHostEnvironmentStatistics? _hostEnvironmentStatistics;
    private readonly Func<EventStoreReceiverSettings, string, ILogger, IEventStoreReceiver> _eventStoreReceiverFactory;

    private IEventStoreQueueCache? _cache;
    private IEventStoreReceiver? _receiver;
    private IStreamQueueCheckpointer<string>? _checkpointer;
    private AggregatedQueueFlowController? _flowController;

    // Receiver life cycle
    private int _receiverState = ReceiverShutdown;

    private const int ReceiverShutdown = 0;
    private const int ReceiverRunning = 1;

    public EventStoreQueueAdapterReceiver(EventStoreReceiverSettings settings, Func<string, IStreamQueueCheckpointer<string>, ILoggerFactory, IEventStoreQueueCache> cacheFactory, Func<string, Task<IStreamQueueCheckpointer<string>>> checkpointerFactory, ILoggerFactory loggerFactory, IQueueAdapterReceiverMonitor receiverMonitor, LoadSheddingOptions loadSheddingOptions, IHostEnvironmentStatistics? hostEnvironmentStatistics, Func<EventStoreReceiverSettings, string, ILogger, IEventStoreReceiver>? eventStoreReceiverFactory = null)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(cacheFactory, nameof(cacheFactory));
        ArgumentNullException.ThrowIfNull(checkpointerFactory, nameof(checkpointerFactory));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        ArgumentNullException.ThrowIfNull(receiverMonitor, nameof(receiverMonitor));
        ArgumentNullException.ThrowIfNull(loadSheddingOptions, nameof(loadSheddingOptions));
        _settings = settings;
        _cacheFactory = cacheFactory;
        _checkpointerFactory = checkpointerFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<EventStoreQueueAdapterReceiver>();
        _receiverMonitor = receiverMonitor;
        _loadSheddingOptions = loadSheddingOptions;
        _hostEnvironmentStatistics = hostEnvironmentStatistics;
        _eventStoreReceiverFactory = eventStoreReceiverFactory ?? CreateReceiver;
    }

    private static IEventStoreReceiver CreateReceiver(EventStoreReceiverSettings settings, string position, ILogger logger)
    {
        return new EventStorePersistentSubscriptionReceiver(settings, position, logger);
    }

    #region IQueueAdapterReceiver Implementation

    /// <inheritdoc />
    public Task Initialize(TimeSpan timeout)
    {
        _logger.LogInformation("Initializing EventStore persistent subscriptions from {ConsumerGroup}-{StreamName}.", _settings.ConsumerGroup, _settings.QueueName);
        // if receiver was already running, do nothing
        return ReceiverRunning == Interlocked.Exchange(ref _receiverState, ReceiverRunning) ? Task.CompletedTask : Initialize();
    }

    /// <summary>
    ///     Initialization of EventStore receiver is performed at adapter receiver initialization, but if it fails,
    ///     it will be retried when messages are requested
    /// </summary>
    /// <returns></returns>
    private async Task Initialize()
    {
        var watch = Stopwatch.StartNew();
        try
        {
            _checkpointer = await _checkpointerFactory(_settings.QueueName);
            if (_cache != null)
            {
                _cache.Dispose();
                _cache = null;
            }
            _cache = _cacheFactory(_settings.QueueName, _checkpointer, _loggerFactory);
            _flowController = new AggregatedQueueFlowController(MaxMessagesPerRead)
                              {
                                  _cache,
                                  LoadShedQueueFlowController.CreateAsPercentOfLoadSheddingLimit(_loadSheddingOptions, _hostEnvironmentStatistics)
                              };
            var position = await _checkpointer.Load();
            _receiver = _eventStoreReceiverFactory(_settings, position, _logger);
            await _receiver.InitAsync();
            watch.Stop();
            _receiverMonitor.TrackInitialization(true, watch.Elapsed, null);
        }
        catch (Exception ex)
        {
            watch.Stop();
            _receiverMonitor.TrackInitialization(false, watch.Elapsed, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task Shutdown(TimeSpan timeout)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            // if receiver was already shutdown, do nothing
            if (ReceiverShutdown == Interlocked.Exchange(ref _receiverState, ReceiverShutdown))
            {
                return;
            }
            _logger.LogInformation("Stopping reading from EventStore persistent subscriptions from {ConsumerGroup}-{StreamName}.", _settings.ConsumerGroup, _settings.QueueName);
            // clear cache and receiver
            var localCache = Interlocked.Exchange(ref _cache, null);
            var localReceiver = Interlocked.Exchange(ref _receiver, null);
            // start closing receiver
            var closeTask = Task.CompletedTask;
            if (localReceiver != null)
            {
                closeTask = localReceiver.CloseAsync();
            }
            // dispose of cache
            localCache?.Dispose();
            // finish return receiver closing task
            await closeTask;
            watch.Stop();
            _receiverMonitor.TrackShutdown(true, watch.Elapsed, null);
        }
        catch (Exception ex)
        {
            watch.Stop();
            _receiverMonitor.TrackShutdown(false, watch.Elapsed, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        if (_receiverState == ReceiverShutdown || maxCount <= 0)
        {
            return new List<IBatchContainer>();
        }
        // if receiver initialization failed, retry
        if (_receiver == null)
        {
            _logger.LogWarning(EventStoreErrorCodes.CannotInitializeSubscriptionClient, "Retrying initialization of EventStore persistent subscriptions from {ConsumerGroup}-{StreamName}.", _settings.ConsumerGroup, _settings.QueueName);
            await Initialize();
            if (_receiver == null)
            {
                // should not get here, should throw instead, but just incase.
                return new List<IBatchContainer>();
            }
        }
        var watch = Stopwatch.StartNew();
        List<EventRecord> messages;
        try
        {
            messages = await _receiver.ReceiveAsync(maxCount);
            watch.Stop();
            _receiverMonitor.TrackRead(true, watch.Elapsed, null);
        }
        catch (Exception ex)
        {
            watch.Stop();
            _receiverMonitor.TrackRead(false, watch.Elapsed, ex);
            _logger.LogWarning(EventStoreErrorCodes.CannotReadFromSubscription, "Failed to read from EventStore persistent subscriptions from {ConsumerGroup}-{StreamName}. Exception: {Exception}", _settings.ConsumerGroup, _settings.QueueName, ex);
            throw;
        }
        var batches = new List<IBatchContainer>();
        if (messages == null || messages.Count == 0)
        {
            _receiverMonitor.TrackMessagesReceived(0, null, null);
            return batches;
        }
        // receiverMonitor message age
        var dequeueTimeUtc = DateTime.UtcNow;
        var oldestMessageEnqueueTime = messages[0].Created;
        var newestMessageEnqueueTime = messages[^1].Created;
        _receiverMonitor.TrackMessagesReceived(messages.Count, oldestMessageEnqueueTime, newestMessageEnqueueTime);
        if (_cache != null)
        {
            var messageStreamPositions = _cache.Add(messages, dequeueTimeUtc);
            batches.AddRange(messageStreamPositions.Select(streamPosition => new StreamActivityNotificationBatch(streamPosition)));
        }
        if (_checkpointer is { CheckpointExists: false })
        {
            _checkpointer.Update(messages[0].EventNumber.ToString(), DateTime.UtcNow);
        }
        return batches;
    }

    /// <inheritdoc />
    public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region IQueueCache Implementation

    /// <inheritdoc />
    public int GetMaxAddCount()
    {
        return _flowController?.GetMaxAddCount() ?? 0;
    }

    /// <inheritdoc />
    public void AddToCache(IList<IBatchContainer> messages)
    {
        // do nothing, we add data directly into cache.  No need for agent involvement
    }

    /// <inheritdoc />
    public bool TryPurgeFromCache(out IList<IBatchContainer>? purgedItems)
    {
        purgedItems = null;
        // if not under pressure, signal the cache to do a time based purge
        // if under pressure, which means consuming speed is less than producing speed, then shouldn't purge, and don't read more message into the cache
        if (!IsUnderPressure())
        {
            _cache?.SignalPurge();
        }
        return false;
    }

    /// <inheritdoc />
    public IQueueCacheCursor GetCacheCursor(StreamId streamId, StreamSequenceToken token)
    {
        return new Cursor(_cache!, streamId, token);
    }

    /// <inheritdoc />
    public bool IsUnderPressure()
    {
        return GetMaxAddCount() <= 0;
    }

    #endregion

    #region Internal Class

    [GenerateSerializer]
    internal class StreamActivityNotificationBatch : IBatchContainer
    {
        public StreamActivityNotificationBatch(StreamPosition position)
        {
            Position = position;
        }

        [Id(0)]
        public StreamPosition Position { get; }

        public StreamId StreamId => Position.StreamId;

        public StreamSequenceToken SequenceToken => Position.SequenceToken;

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        {
            throw new NotSupportedException();
        }

        public bool ImportRequestContext()
        {
            throw new NotSupportedException();
        }
    }

    private class Cursor : IQueueCacheCursor
    {
        private readonly IEventStoreQueueCache _cache;
        private readonly object _cursor;
        private IBatchContainer _current;

        public Cursor(IEventStoreQueueCache cache, StreamId streamId, StreamSequenceToken token)
        {
            _cache = cache;
            _cursor = cache.GetCursor(streamId, token);
            _current = null!;
        }

        public void Dispose()
        {
        }

        public IBatchContainer GetCurrent(out Exception? exception)
        {
            exception = null;
            return _current;
        }

        public bool MoveNext()
        {
            if (!_cache.TryGetNextMessage(_cursor, out var next))
            {
                return false;
            }
            _current = next;
            return true;
        }

        public void Refresh(StreamSequenceToken token)
        {
        }

        public void RecordDeliveryFailure()
        {
        }
    }

    #endregion

}
