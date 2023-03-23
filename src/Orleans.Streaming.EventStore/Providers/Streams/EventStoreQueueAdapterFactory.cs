using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Providers.Streams.EventStore.StatisticMonitors;
using Orleans.Runtime;
using Orleans.Statistics;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Adapter factory. This should create an adapter from the stream provider configuration
/// </summary>
public class EventStoreQueueAdapterFactory : IQueueAdapterFactory
{
    private readonly string _name;
    private readonly EventStoreOptions _options;
    private readonly EventStoreStreamCachePressureOptions _cacheOptions;
    private readonly StreamCacheEvictionOptions _cacheEvictionOptions;
    private readonly StreamStatisticOptions _statisticOptions;
    private readonly IEventStoreDataAdapter _dataAdapter;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostEnvironmentStatistics _hostEnvironmentStatistics;

    private HashRingBasedPartitionedStreamQueueMapper? _streamQueueMapper;

    private string[] _queues;

    /// <summary>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static EventStoreQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
    {
        var storageOptions = serviceProvider.GetOptionsByName<EventStoreOptions>(name);
        var clusterOptions = serviceProvider.GetRequiredService<IOptions<ClusterOptions>>();
        var cacheOptions = serviceProvider.GetOptionsByName<SimpleQueueCacheOptions>(name);
        var dataAdapter = serviceProvider.GetServiceByName<IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>>(name) ?? serviceProvider.GetRequiredService<IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>>();
        var queueAdapterFactory = ActivatorUtilities.CreateInstance<EventStoreQueueAdapterFactory>(serviceProvider, name, storageOptions, clusterOptions, cacheOptions, dataAdapter);
        queueAdapterFactory.Init();
        return queueAdapterFactory;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreQueueAdapterFactory" /> class.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <param name="receiverOptions"></param>
    /// <param name="cacheOptions"></param>
    /// <param name="statisticOptions"></param>
    /// <param name="dataAdapter"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="cacheEvictionOptions"></param>
    /// <param name="hostEnvironmentStatistics"></param>
    public EventStoreQueueAdapterFactory(string name,
                                         EventStoreOptions options,
                                         EventStoreReceiverOptions receiverOptions,
                                         EventStoreStreamCachePressureOptions cacheOptions,
                                         StreamCacheEvictionOptions cacheEvictionOptions,
                                         StreamStatisticOptions statisticOptions,
                                         IEventStoreDataAdapter dataAdapter,
                                         IServiceProvider serviceProvider,
                                         ILoggerFactory loggerFactory,
                                         IHostEnvironmentStatistics hostEnvironmentStatistics)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(receiverOptions, nameof(receiverOptions));
        ArgumentNullException.ThrowIfNull(cacheOptions, nameof(cacheOptions));
        ArgumentNullException.ThrowIfNull(cacheEvictionOptions, nameof(cacheEvictionOptions));
        ArgumentNullException.ThrowIfNull(statisticOptions, nameof(statisticOptions));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        _name = name;
        _options = options;
        _cacheOptions = cacheOptions;
        _cacheEvictionOptions = cacheEvictionOptions;
        _statisticOptions = statisticOptions;
        _dataAdapter = dataAdapter;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _hostEnvironmentStatistics = hostEnvironmentStatistics;
    }

    /// <summary>
    ///     Creates a message cache for an EventStore queue.
    /// </summary>
    protected Func<string, IStreamQueueCheckpointer<string>, ILoggerFactory, IEventStoreQueueCache>? CacheFactory { get; set; }

    /// <summary>
    ///     Creates a queue checkpointer.
    /// </summary>
    private IStreamQueueCheckpointerFactory _checkpointerFactory;

    /// <summary>
    ///     Creates a failure handler for a queue.
    /// </summary>
    protected Func<string, Task<IStreamFailureHandler>>? StreamFailureHandlerFactory { get; set; }

    /// <summary>
    ///     Create a queue mapper to map EventStore streams to queues.
    /// </summary>
    protected Func<string[], HashRingBasedPartitionedStreamQueueMapper>? QueueMapperFactory { get; set; }

    /// <summary>
    ///     Create a receiver monitor to report performance metrics.
    ///     Factory function should return an IEventStoreReceiverMonitor.
    /// </summary>
    protected Func<EventStoreReceiverMonitorDimensions, ILoggerFactory, IQueueAdapterReceiverMonitor>? ReceiverMonitorFactory { get; set; }

    /// <summary>
    /// </summary>
    public virtual void Init()
    {
        CacheFactory ??= CreateCacheFactory(_cacheOptions).CreateCache;
        StreamFailureHandlerFactory ??= queue => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
        QueueMapperFactory ??= queues => new HashRingBasedPartitionedStreamQueueMapper(queues, _name);
        ReceiverMonitorFactory ??= (dimensions, logger) => new DefaultEventStoreReceiverMonitor(dimensions);
    }

    #region IQueueAdapterFactory Implementation

    /// <inheritdoc />
    public async Task<IQueueAdapter> CreateAdapter()
    {
        return null;
    }

    /// <inheritdoc />
    public IQueueAdapterCache GetQueueAdapterCache()
    {
        return null;
    }

    /// <inheritdoc />
    public IStreamQueueMapper GetStreamQueueMapper()
    {
        if (_streamQueueMapper == null)
        {
            _queues = GetQueueNames();
            _streamQueueMapper = QueueMapperFactory?.Invoke(_queues) ?? new HashRingBasedPartitionedStreamQueueMapper(_queues, _name);
        }
        return _streamQueueMapper;
    }

    /// <inheritdoc />
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        if (_streamQueueMapper != null && StreamFailureHandlerFactory != null)
        {
            var queue = _streamQueueMapper.QueueToPartition(queueId);
            return StreamFailureHandlerFactory.Invoke(queue);
        }
        return Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
    }

    #endregion

    /// <summary>
    ///     Get queue Ids from EventStore
    /// </summary>
    /// <returns></returns>
    protected virtual string[] GetQueueNames()
    {
        return Array.Empty<string>();
    }

    /// <summary>
    ///     Create a IEventStoreQueueCacheFactory. It will create a EventStoreQueueCacheFactory by default.
    ///     User can override this function to return their own implementation of IEventStoreQueueCacheFactory,
    ///     and other customization of IEventStoreQueueCacheFactory if they may.
    /// </summary>
    /// <returns></returns>
    protected virtual IEventStoreQueueCacheFactory CreateCacheFactory(EventStoreStreamCachePressureOptions eventStoreCacheOptions)
    {
        var sharedDimensions = new EventStoreMonitorAggregationDimensions(_options.EventStoreName);
        return new EventStoreQueueCacheFactory(eventStoreCacheOptions, _cacheEvictionOptions, _statisticOptions, _dataAdapter, sharedDimensions);
    }
}
