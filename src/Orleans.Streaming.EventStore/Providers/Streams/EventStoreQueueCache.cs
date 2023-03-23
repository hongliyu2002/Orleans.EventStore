using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using StreamPosition = Orleans.Streams.StreamPosition;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     EventStore queue cache.
/// </summary>
public class EventStoreQueueCache : IEventStoreQueueCache
{
    private readonly int _defaultMaxAddCount;
    private readonly IObjectPool<FixedSizeBuffer> _bufferPool;
    private readonly IEventStoreDataAdapter _dataAdapter;
    private readonly IEvictionStrategy _evictionStrategy;
    private readonly IStreamQueueCheckpointer<string> _checkpointer;
    private readonly ILogger _logger;
    private readonly AggregatedCachePressureMonitor _cachePressureMonitor;
    private readonly ICacheMonitor _cacheMonitor;
    private FixedSizeBuffer? _buffer;

    private readonly PooledQueueCache _cache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreQueueCache" /> class.
    /// </summary>
    /// <param name="queue">Partition this instance is caching.</param>
    /// <param name="defaultMaxAddCount">Default max number of items that can be added to the cache between purge calls.</param>
    /// <param name="bufferPool">raw data block pool.</param>
    /// <param name="dataAdapter">Adapts EventRecord to cached.</param>
    /// <param name="evictionStrategy">Eviction strategy manage purge related events</param>
    /// <param name="checkpointer"></param>
    /// <param name="logger"></param>
    /// <param name="cacheMonitor"></param>
    /// <param name="cacheMonitorWriteInterval"></param>
    /// <param name="purgeMetadataInterval"></param>
    public EventStoreQueueCache(string queue,
                                int defaultMaxAddCount,
                                IObjectPool<FixedSizeBuffer> bufferPool,
                                IEventStoreDataAdapter dataAdapter,
                                IEvictionStrategy evictionStrategy,
                                IStreamQueueCheckpointer<string> checkpointer,
                                ILogger logger,
                                ICacheMonitor cacheMonitor,
                                TimeSpan? cacheMonitorWriteInterval,
                                TimeSpan? purgeMetadataInterval)
    {
        Partition = queue;
        _defaultMaxAddCount = defaultMaxAddCount;
        _bufferPool = bufferPool;
        _dataAdapter = dataAdapter;
        _evictionStrategy = evictionStrategy;
        _evictionStrategy.OnPurged = OnPurged;
        _evictionStrategy.PurgeObservable = _cache;
        _checkpointer = checkpointer;
        _logger = logger;
        _cache = new PooledQueueCache(dataAdapter, logger, cacheMonitor, cacheMonitorWriteInterval, purgeMetadataInterval);
        _cacheMonitor = cacheMonitor;
        _cachePressureMonitor = new AggregatedCachePressureMonitor(logger, cacheMonitor);
    }

    /// <summary>
    ///     Partition this instance is caching.
    /// </summary>
    public string Partition { get; }

    /// <summary>
    ///     Handles cache purge signals
    /// </summary>
    /// <param name="lastItemPurged"></param>
    /// <param name="newestItem"></param>
    private void OnPurged(CachedMessage? lastItemPurged, CachedMessage? newestItem)
    {
        if (_logger.IsEnabled(LogLevel.Debug) && lastItemPurged.HasValue && newestItem.HasValue)
        {
            _logger.LogDebug("CachePeriod: EnqueueTimeUtc: {OldestEnqueueTimeUtc} to {NewestEnqueueTimeUtc}, DequeueTimeUtc: {OldestDequeueTimeUtc} to {NewestDequeueTimeUtc}",
                             LogFormatter.PrintDate(lastItemPurged.Value.EnqueueTimeUtc),
                             LogFormatter.PrintDate(newestItem.Value.EnqueueTimeUtc),
                             LogFormatter.PrintDate(lastItemPurged.Value.DequeueTimeUtc),
                             LogFormatter.PrintDate(newestItem.Value.DequeueTimeUtc));
        }
        if (lastItemPurged.HasValue)
        {
            _checkpointer.Update(_dataAdapter.GetPosition(lastItemPurged.Value), DateTime.UtcNow);
        }
    }

    #region IEventStoreQueueCache Implementation

    /// <summary>
    ///     Add a list of EventStore EventRecord to the cache.
    /// </summary>
    /// <param name="queueMessages"></param>
    /// <param name="dequeueTimeUtc"></param>
    /// <returns></returns>
    public List<StreamPosition> Add(List<EventRecord> queueMessages, DateTime dequeueTimeUtc)
    {
        var streamPositions = new List<StreamPosition>(queueMessages.Count);
        var cachedMessages = new List<CachedMessage>(queueMessages.Count);
        foreach (var queueMessage in queueMessages)
        {
            var streamPosition = _dataAdapter.GetStreamPosition(queueMessage);
            cachedMessages.Add(_dataAdapter.FromQueueMessage(streamPosition, queueMessage, dequeueTimeUtc, GetSegment));
            streamPositions.Add(streamPosition);
        }
        _cache.Add(cachedMessages, dequeueTimeUtc);
        return streamPositions;
    }

    private ArraySegment<byte> GetSegment(int size)
    {
        // get segment from current block
        if (_buffer == null || !_buffer.TryGetSegment(size, out var segment))
        {
            // no block or block full, get new block and try again
            _buffer = _bufferPool.Allocate();
            //call EvictionStrategy's OnBlockAllocated method
            _evictionStrategy.OnBlockAllocated(_buffer);
            // if this fails with clean block, then requested size is too big
            if (!_buffer.TryGetSegment(size, out segment))
            {
                throw new ArgumentOutOfRangeException(nameof(size), $"Message size is to big. MessageSize: {size}");
            }
        }
        return segment;
    }

    /// <summary>
    ///     Get a cursor into the cache to read events from a stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="sequenceToken"></param>
    /// <returns></returns>
    public object GetCursor(StreamId streamId, StreamSequenceToken sequenceToken)
    {
        return _cache.GetCursor(streamId, sequenceToken);
    }

    /// <summary>
    ///     Try to get the next message in the cache for the provided cursor.
    /// </summary>
    /// <param name="cursorObj"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    public bool TryGetNextMessage(object cursorObj, out IBatchContainer container)
    {
        if (!_cache.TryGetNextMessage(cursorObj, out container))
        {
            return false;
        }
        _cachePressureMonitor.RecordCachePressureContribution(TryCalculateCachePressureContribution(container.SequenceToken, out var cachePressureContribution) ? cachePressureContribution : 0.0);
        return true;
    }

    /// <summary>
    ///     cachePressureContribution should be a double between 0-1, indicating how much danger the item is of being removed from the cache.
    ///     0 indicating  no danger,
    ///     1 indicating removal is imminent.
    /// </summary>
    private bool TryCalculateCachePressureContribution(StreamSequenceToken sequenceToken, out double cachePressureContribution)
    {
        cachePressureContribution = 0;
        // if cache is empty or has few items, don't calculate pressure
        if (_cache.IsEmpty || !_cache.Newest.HasValue || !_cache.Oldest.HasValue || _cache.Newest.Value.SequenceNumber - _cache.Oldest.Value.SequenceNumber < 10 * _defaultMaxAddCount)
        {
            // not enough items in cache.
            return false;
        }
        var location = (IEventStoreLocation)sequenceToken;
        double cacheSize = _cache.Newest.Value.SequenceNumber - _cache.Oldest.Value.SequenceNumber;
        var distanceFromNewestMessage = _cache.Newest.Value.SequenceNumber - location.SequenceNumber;
        // pressure is the ratio of the distance from the front of the cache to the
        cachePressureContribution = distanceFromNewestMessage / cacheSize;
        return true;
    }

    /// <summary>
    ///     Add cache pressure monitor to the cache's back pressure algorithm
    /// </summary>
    /// <param name="monitor"></param>
    public void AddCachePressureMonitor(ICachePressureMonitor monitor)
    {
        monitor.CacheMonitor = _cacheMonitor;
        _cachePressureMonitor.AddCachePressureMonitor(monitor);
    }

    /// <inheritdoc />
    public void SignalPurge()
    {
        _evictionStrategy.PerformPurge(DateTime.UtcNow);
    }

    #endregion

    #region IQueueFlowController Implementation

    /// <summary>
    ///     The limit of the maximum number of items that can be added
    /// </summary>
    public int GetMaxAddCount()
    {
        return _cachePressureMonitor.IsUnderPressure(DateTime.UtcNow) ? 0 : _defaultMaxAddCount;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public void Dispose()
    {
        _evictionStrategy.OnPurged = null;
    }

    #endregion

}
