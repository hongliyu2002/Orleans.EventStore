using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streaming.Configuration;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Adapter factory. This should create an adapter from the stream provider configuration
/// </summary>
public class EventStoreQueueAdapterFactory : IQueueAdapterFactory
{
    private readonly string _name;
    private readonly EventStoreStorageOptions _storageOptions;
    private readonly IOptions<ClusterOptions> _clusterOptions;
    private readonly IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> _dataAdapter;
    private readonly ILoggerFactory _loggerFactory;

    private readonly HashRingBasedPartitionedStreamQueueMapper _streamQueueMapper;
    private readonly SimpleQueueAdapterCache _adapterCache;

    /// <summary>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static EventStoreQueueAdapterFactory Create(IServiceProvider serviceProvider, string name)
    {
        var storageOptions = serviceProvider.GetOptionsByName<EventStoreStorageOptions>(name);
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
    /// <param name="storageOptions"></param>
    /// <param name="clusterOptions"></param>
    /// <param name="cacheOptions"></param>
    /// <param name="dataAdapter"></param>
    /// <param name="loggerFactory"></param>
    public EventStoreQueueAdapterFactory(string name,
                                         EventStoreStorageOptions storageOptions,
                                         IOptions<ClusterOptions> clusterOptions,
                                         SimpleQueueCacheOptions cacheOptions,
                                         IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> dataAdapter,
                                         ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(storageOptions, nameof(storageOptions));
        ArgumentNullException.ThrowIfNull(clusterOptions, nameof(clusterOptions));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        _name = name;
        _storageOptions = storageOptions;
        _clusterOptions = clusterOptions;
        _dataAdapter = dataAdapter;
        _loggerFactory = loggerFactory;
        _streamQueueMapper = new HashRingBasedPartitionedStreamQueueMapper(storageOptions.QueueNames, name);
        _adapterCache = new SimpleQueueAdapterCache(cacheOptions, name, loggerFactory);
    }

    /// <summary>
    ///     Application level failure handler override.
    /// </summary>
    protected Func<QueueId, Task<IStreamFailureHandler>>? StreamFailureHandlerFactory { private get; set; }

    /// <summary>
    ///     Init the factory.
    /// </summary>
    public virtual void Init()
    {
        StreamFailureHandlerFactory ??= _ => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
    }

    /// <summary>
    ///     Creates a queue adapter.
    /// </summary>
    /// <returns>The queue adapter</returns>
    public Task<IQueueAdapter> CreateAdapter()
    {
        var queueAdapter = new EventStoreQueueAdapter(_name, _storageOptions, _clusterOptions, _streamQueueMapper, _dataAdapter, _loggerFactory);
        return Task.FromResult<IQueueAdapter>(queueAdapter);
    }

    /// <summary>
    ///     Creates queue message cache adapter.
    /// </summary>
    /// <returns>The queue adapter cache.</returns>
    public IQueueAdapterCache GetQueueAdapterCache()
    {
        return _adapterCache;
    }

    /// <summary>
    ///     Creates a queue mapper.
    /// </summary>
    /// <returns>The queue mapper.</returns>
    public IStreamQueueMapper GetStreamQueueMapper()
    {
        return _streamQueueMapper;
    }

    /// <summary>
    ///     Acquire delivery failure handler for a queue
    /// </summary>
    /// <param name="queueId">The queue identifier.</param>
    /// <returns>The stream failure handler.</returns>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return StreamFailureHandlerFactory?.Invoke(queueId) ?? Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler());
    }
}
