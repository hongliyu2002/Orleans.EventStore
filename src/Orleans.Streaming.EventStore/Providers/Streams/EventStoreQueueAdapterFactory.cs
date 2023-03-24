using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Providers.Streams.EventStore.StatisticMonitors;
using Orleans.Runtime;
using Orleans.Statistics;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Adapter factory. This should create an adapter from the stream provider configuration.
/// </summary>
public class EventStoreQueueAdapterFactory : IQueueAdapterFactory
{
    private readonly string _name;
    private readonly EventStoreOptions _options;
    private readonly EventStoreReceiverOptions _receiverOptions;
    private readonly StreamCacheEvictionOptions _cacheEvictionOptions;
    private readonly StreamStatisticOptions _statisticOptions;
    private readonly IEventStoreDataAdapter _dataAdapter;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostEnvironmentStatistics? _hostEnvironmentStatistics;

    private HashRingBasedPartitionedStreamQueueMapper? _streamQueueMapper;
    private EventStoreQueueAdapter? _queueAdapter;

    /// <summary>
    ///     Creates an instance of the <see cref="EventStoreQueueAdapterFactory" /> using the provided IServiceProvider and name.
    /// </summary>
    /// <param name="serviceProvider">The IServiceProvider used for dependency injection.</param>
    /// <param name="name">The name used to retrieve options and services from the IServiceProvider.</param>
    /// <returns>An instance of the <see cref="EventStoreQueueAdapterFactory" />.</returns>
    public static EventStoreQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
    {
        var options = serviceProvider.GetOptionsByName<EventStoreOptions>(name);
        var receiverOptions = serviceProvider.GetOptionsByName<EventStoreReceiverOptions>(name);
        var cachePressureOptions = serviceProvider.GetOptionsByName<EventStoreStreamCachePressureOptions>(name);
        var cacheEvictionOptions = serviceProvider.GetOptionsByName<StreamCacheEvictionOptions>(name);
        var statisticOptions = serviceProvider.GetOptionsByName<StreamStatisticOptions>(name);
        var dataAdapter = serviceProvider.GetServiceByName<IEventStoreDataAdapter>(name) ?? serviceProvider.GetService<IEventStoreDataAdapter>() ?? ActivatorUtilities.CreateInstance<EventStoreQueueDataAdapterV2>(serviceProvider);
        return ActivatorUtilities.CreateInstance<EventStoreQueueAdapterFactory>(serviceProvider, name, options, receiverOptions, cachePressureOptions, cacheEvictionOptions, statisticOptions, dataAdapter);
    }

    /// <summary>
    ///     Creates a new instance of <see cref="EventStoreQueueAdapterFactory" />.
    /// </summary>
    /// <param name="name">Name of the adapter. Primarily for logging purposes.</param>
    /// <param name="options">The options for connecting to the event store.</param>
    /// <param name="receiverOptions">The options for receiving events from the event store.</param>
    /// <param name="cachePressureOptions">The options for configuring the stream cache pressure.</param>
    /// <param name="cacheEvictionOptions">The options for configuring stream cache eviction.</param>
    /// <param name="statisticOptions">The options for configuring statistics collection.</param>
    /// <param name="dataAdapter">The adapter for converting between EventStore's data and Orleans' data.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="hostEnvironmentStatistics">The host environment statistics.</param>
    public EventStoreQueueAdapterFactory(string name, EventStoreOptions options, EventStoreReceiverOptions receiverOptions, EventStoreStreamCachePressureOptions cachePressureOptions, StreamCacheEvictionOptions cacheEvictionOptions, StreamStatisticOptions statisticOptions, IEventStoreDataAdapter dataAdapter, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironmentStatistics? hostEnvironmentStatistics)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(receiverOptions, nameof(receiverOptions));
        ArgumentNullException.ThrowIfNull(cachePressureOptions, nameof(cachePressureOptions));
        ArgumentNullException.ThrowIfNull(cacheEvictionOptions, nameof(cacheEvictionOptions));
        ArgumentNullException.ThrowIfNull(statisticOptions, nameof(statisticOptions));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        _name = name;
        _options = options;
        _receiverOptions = receiverOptions;
        _cacheEvictionOptions = cacheEvictionOptions;
        _statisticOptions = statisticOptions;
        _dataAdapter = dataAdapter;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _hostEnvironmentStatistics = hostEnvironmentStatistics;
        CacheFactory = CreateCacheFactory(cachePressureOptions).CreateCache;
        StreamFailureHandlerFactory = _ => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
        QueueMapperFactory = queues => new HashRingBasedPartitionedStreamQueueMapper(queues, name);
        ReceiverMonitorFactory = (dimensions, _) => new DefaultEventStoreReceiverMonitor(dimensions);
        ReceiverFactory = (settings, position, logger) => new EventStorePersistentSubscriptionReceiver(settings, position, logger);
    }

    /// <summary>
    ///     Creates a message cache for an EventStore queue.
    /// </summary>
    protected Func<string, IStreamQueueCheckpointer<string>, ILoggerFactory, IEventStoreQueueCache> CacheFactory { get; set; }

    /// <summary>
    ///     Creates a failure handler for a queue.
    /// </summary>
    protected Func<string, Task<IStreamFailureHandler>> StreamFailureHandlerFactory { get; set; }

    /// <summary>
    ///     Create a queue mapper to map EventStore streams to queues.
    /// </summary>
    protected Func<string[], HashRingBasedPartitionedStreamQueueMapper> QueueMapperFactory { get; set; }

    /// <summary>
    ///     Create a receiver monitor to report performance metrics.
    ///     Factory function should return an IEventStoreReceiverMonitor.
    /// </summary>
    protected Func<EventStoreReceiverMonitorDimensions, ILoggerFactory, IQueueAdapterReceiverMonitor> ReceiverMonitorFactory { get; set; }

    /// <summary>
    ///     Factory to create a IEventStoreReceiver.
    ///     for testing purpose, used in EventStoreGeneratorStreamProvider
    /// </summary>
    protected Func<EventStoreReceiverSettings, string, ILogger, IEventStoreReceiver> ReceiverFactory { get; set; }

    /// <summary>
    ///     Create a IEventStoreQueueCacheFactory. It will create a EventStoreQueueCacheFactory by default.
    ///     User can override this function to return their own implementation of IEventStoreQueueCacheFactory,
    ///     and other customization of IEventStoreQueueCacheFactory if they may.
    /// </summary>
    /// <returns></returns>
    protected virtual IEventStoreQueueCacheFactory CreateCacheFactory(EventStoreStreamCachePressureOptions cachePressureOptions)
    {
        var sharedDimensions = new EventStoreMonitorAggregationDimensions(_options.Name);
        return new EventStoreQueueCacheFactory(cachePressureOptions, _cacheEvictionOptions, _statisticOptions, _dataAdapter, sharedDimensions);
    }

    /// <summary>
    ///     Get queue names (aka stream names) from EventStore.
    /// </summary>
    /// <returns></returns>
    protected virtual string[] GetQueueNames()
    {
        return _options.Queues.ToArray();
    }

    #region IQueueAdapterFactory Implementation

    /// <summary>
    ///     Creates a queue adapter.
    /// </summary>
    /// <returns>The queue adapter</returns>
    public Task<IQueueAdapter> CreateAdapter()
    {
        return Task.FromResult<IQueueAdapter>(GetOrCreateAdapter());
    }

    private EventStoreQueueAdapter GetOrCreateAdapter()
    {
        if (_queueAdapter == null)
        {
            var streamQueueMapper = GetOrCreateStreamQueueMapper();
            _queueAdapter = new EventStoreQueueAdapter(_name, _options, _receiverOptions, streamQueueMapper, CacheFactory, ReceiverMonitorFactory, ReceiverFactory, _dataAdapter, _serviceProvider, _loggerFactory, _hostEnvironmentStatistics);
        }
        return _queueAdapter;
    }

    /// <summary>
    ///     Creates queue message cache adapter.
    /// </summary>
    /// <returns>The queue adapter cache.</returns>
    public IQueueAdapterCache GetQueueAdapterCache()
    {
        return GetOrCreateAdapter();
    }

    /// <summary>
    ///     Creates a queue mapper.
    /// </summary>
    /// <returns>The queue mapper.</returns>
    public IStreamQueueMapper GetStreamQueueMapper()
    {
        return GetOrCreateStreamQueueMapper();
    }

    private HashRingBasedPartitionedStreamQueueMapper GetOrCreateStreamQueueMapper()
    {
        if (_streamQueueMapper == null)
        {
            _streamQueueMapper = QueueMapperFactory.Invoke(GetQueueNames());
        }
        return _streamQueueMapper;
    }

    /// <summary>
    ///     Acquire delivery failure handler for a queue
    /// </summary>
    /// <param name="queueId">The queue identifier.</param>
    /// <returns>The stream failure handler.</returns>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        if (_streamQueueMapper != null)
        {
            var queue = _streamQueueMapper.QueueToPartition(queueId);
            return StreamFailureHandlerFactory.Invoke(queue);
        }
        return Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
    }

    #endregion

}
