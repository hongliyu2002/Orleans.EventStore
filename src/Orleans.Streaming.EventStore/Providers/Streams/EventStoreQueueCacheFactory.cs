using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Providers.Streams.EventStore.StatisticMonitors;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Factory class to configure and create IEventStoreQueueCache
/// </summary>
public class EventStoreQueueCacheFactory : IEventStoreQueueCacheFactory
{
    private readonly EventStoreStreamCachePressureOptions _cacheOptions;
    private readonly StreamCacheEvictionOptions _evictionOptions;
    private readonly StreamStatisticOptions _statisticOptions;
    private readonly IEventStoreDataAdapter _dataAdater;
    private readonly EventStoreMonitorAggregationDimensions _sharedDimensions;
    private readonly TimePurgePredicate _timePurge;

    private IObjectPool<FixedSizeBuffer>? _bufferPool;
    private string? _bufferPoolId;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreQueueCacheFactory" /> class.
    /// </summary>
    public EventStoreQueueCacheFactory(EventStoreStreamCachePressureOptions cacheOptions,
                                       StreamCacheEvictionOptions evictionOptions,
                                       StreamStatisticOptions statisticOptions,
                                       IEventStoreDataAdapter dataAdater,
                                       EventStoreMonitorAggregationDimensions sharedDimensions,
                                       Func<EventStoreCacheMonitorDimensions, ILoggerFactory, ICacheMonitor>? cacheMonitorFactory = null,
                                       Func<EventStoreBlockPoolMonitorDimensions, ILoggerFactory, IBlockPoolMonitor>? blockPoolMonitorFactory = null)
    {
        _cacheOptions = cacheOptions;
        _evictionOptions = evictionOptions;
        _statisticOptions = statisticOptions;
        _dataAdater = dataAdater;
        _sharedDimensions = sharedDimensions;
        _timePurge = new TimePurgePredicate(evictionOptions.DataMinTimeInCache, evictionOptions.DataMaxAgeInCache);
        CacheMonitorFactory = cacheMonitorFactory ?? ((dimensions, _) => new DefaultEventStoreCacheMonitor(dimensions));
        BlockPoolMonitorFactory = blockPoolMonitorFactory ?? ((dimensions, _) => new DefaultEventStoreBlockPoolMonitor(dimensions));
    }

    /// <summary>
    ///     Create a cache monitor to report performance metrics.
    ///     Factory function should return an ICacheMonitor.
    /// </summary>
    public Func<EventStoreCacheMonitorDimensions, ILoggerFactory, ICacheMonitor> CacheMonitorFactory { get; set; }

    /// <summary>
    ///     Create a block pool monitor to report performance metrics.
    ///     Factory function should return an IObjectPoolMonitor.
    /// </summary>
    public Func<EventStoreBlockPoolMonitorDimensions, ILoggerFactory, IBlockPoolMonitor> BlockPoolMonitorFactory { get; set; }

    /// <summary>
    ///     Function which create an EventStoreQueueCache, which by default will configure the EventStoreQueueCache using configuration in CreateBufferPool function
    ///     and AddCachePressureMonitors function.
    /// </summary>
    /// <returns></returns>
    public IEventStoreQueueCache CreateCache(string partition, IStreamQueueCheckpointer<string> checkpointer, ILoggerFactory loggerFactory)
    {
        var blockPool = CreateBufferPool(_statisticOptions, loggerFactory, _sharedDimensions, out var blockPoolId);
        var cache = CreateCache(partition, _dataAdater, _statisticOptions, _evictionOptions, checkpointer, loggerFactory, blockPool, blockPoolId, _timePurge, _sharedDimensions);
        AddCachePressureMonitors(cache, _cacheOptions, loggerFactory.CreateLogger($"{typeof(EventStoreQueueCache).FullName}.{_sharedDimensions.EventStorePath}.{partition}"));
        return cache;
    }

    /// <summary>
    ///     Function used to configure BufferPool for EventStoreQueueCache.
    ///     User can override this function to provide more customization on BufferPool creation
    /// </summary>
    protected virtual IObjectPool<FixedSizeBuffer> CreateBufferPool(StreamStatisticOptions statisticOptions, ILoggerFactory loggerFactory, EventStoreMonitorAggregationDimensions sharedDimensions, out string blockPoolId)
    {
        if (_bufferPool == null)
        {
            var bufferSize = 1 << 20;
            _bufferPoolId = $"BlockPool-{new Guid().ToString()}-BlockSize-{bufferSize}";
            var monitorDimensions = new EventStoreBlockPoolMonitorDimensions(sharedDimensions, _bufferPoolId);
            var objectPoolMonitor = new ObjectPoolMonitorBridge(BlockPoolMonitorFactory(monitorDimensions, loggerFactory), bufferSize);
            _bufferPool = new ObjectPool<FixedSizeBuffer>(() => new FixedSizeBuffer(bufferSize), objectPoolMonitor, statisticOptions.StatisticMonitorWriteInterval);
        }
        blockPoolId = _bufferPoolId!;
        return _bufferPool;
    }

    /// <summary>
    ///     Function used to configure cache pressure monitors for EventStoreQueueCache.
    ///     User can override this function to provide more customization on cache pressure monitors
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="providerOptions"></param>
    /// <param name="cacheLogger"></param>
    protected virtual void AddCachePressureMonitors(IEventStoreQueueCache cache, EventStoreStreamCachePressureOptions providerOptions, ILogger cacheLogger)
    {
        if (providerOptions.AveragingCachePressureMonitorFlowControlThreshold.HasValue)
        {
            var avgMonitor = new AveragingCachePressureMonitor(providerOptions.AveragingCachePressureMonitorFlowControlThreshold.Value, cacheLogger);
            cache.AddCachePressureMonitor(avgMonitor);
        }
        if (!providerOptions.SlowConsumingMonitorPressureWindowSize.HasValue && !providerOptions.SlowConsumingMonitorFlowControlThreshold.HasValue)
        {
            return;
        }
        var slowConsumeMonitor = new SlowConsumingPressureMonitor(cacheLogger);
        if (providerOptions.SlowConsumingMonitorFlowControlThreshold.HasValue)
        {
            slowConsumeMonitor.FlowControlThreshold = providerOptions.SlowConsumingMonitorFlowControlThreshold.Value;
        }
        if (providerOptions.SlowConsumingMonitorPressureWindowSize.HasValue)
        {
            slowConsumeMonitor.PressureWindowSize = providerOptions.SlowConsumingMonitorPressureWindowSize.Value;
        }
        cache.AddCachePressureMonitor(slowConsumeMonitor);
    }

    /// <summary>
    ///     Default function to be called to create an EventhubQueueCache in IEventStoreQueueCacheFactory.CreateCache method. User can
    ///     override this method to add more customization.
    /// </summary>
    protected virtual IEventStoreQueueCache CreateCache(string partition,
                                                        IEventStoreDataAdapter dataAdatper,
                                                        StreamStatisticOptions statisticOptions,
                                                        StreamCacheEvictionOptions streamCacheEvictionOptions,
                                                        IStreamQueueCheckpointer<string> checkpointer,
                                                        ILoggerFactory loggerFactory,
                                                        IObjectPool<FixedSizeBuffer> bufferPool,
                                                        string blockPoolId,
                                                        TimePurgePredicate timePurge,
                                                        EventStoreMonitorAggregationDimensions sharedDimensions)
    {
        var cacheMonitorDimensions = new EventStoreCacheMonitorDimensions(sharedDimensions, partition, blockPoolId);
        var cacheMonitor = CacheMonitorFactory(cacheMonitorDimensions, loggerFactory);
        var logger = loggerFactory.CreateLogger($"{typeof(EventStoreQueueCache).FullName}.{sharedDimensions.EventStorePath}.{partition}");
        var evictionStrategy = new ChronologicalEvictionStrategy(logger, timePurge, cacheMonitor, statisticOptions.StatisticMonitorWriteInterval);
        return new EventStoreQueueCache(partition, EventStoreQueueAdapterReceiver.MaxMessagesPerRead, bufferPool, dataAdatper, evictionStrategy, checkpointer, logger, cacheMonitor, statisticOptions.StatisticMonitorWriteInterval, streamCacheEvictionOptions.MetadataMinTimeInCache);
    }

}
