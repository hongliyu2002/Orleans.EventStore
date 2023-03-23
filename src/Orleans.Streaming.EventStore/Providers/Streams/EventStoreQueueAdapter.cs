using System.Collections.Concurrent;
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
///     Queue adapter factory which allows the PersistentStreamProvider to use EventStore as its backend persistent event queue.
/// </summary>
public class EventStoreQueueAdapter : IQueueAdapter, IQueueAdapterCache
{
    private readonly EventStoreOptions _options;
    private readonly EventStoreReceiverOptions _receiverOptions;
    private readonly HashRingBasedPartitionedStreamQueueMapper _streamQueueMapper;
    private readonly Func<string, IStreamQueueCheckpointer<string>, ILoggerFactory, IEventStoreQueueCache> _cacheFactory;
    private readonly Func<string, Task<IStreamQueueCheckpointer<string>>> _checkpointerFactory;
    private readonly Func<EventStoreReceiverMonitorDimensions, ILoggerFactory, IQueueAdapterReceiverMonitor> _receiverMonitorFactory;
    private readonly IEventStoreDataAdapter _dataAdapter;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostEnvironmentStatistics? _hostEnvironmentStatistics;
    private readonly Func<EventStoreReceiverSettings, string, ILogger, IEventStoreReceiver>? _receiverFactory;

    private readonly ConcurrentDictionary<QueueId, EventStoreProducer> _producers = new();
    private readonly ConcurrentDictionary<QueueId, EventStoreQueueAdapterReceiver> _receivers = new();

    /// <summary>
    ///     Initializes a new instance of the EventStoreQueueAdapter class.
    /// </summary>
    /// <param name="name">The name of the adapter.</param>
    /// <param name="options">The EventStore options.</param>
    /// <param name="receiverOptions">The EventStore receiver options.</param>
    /// <param name="streamQueueMapper">The stream queue mapper.</param>
    /// <param name="cacheFactory">The cache factory.</param>
    /// <param name="checkpointerFactory">The checkpointer factory.</param>
    /// <param name="receiverMonitorFactory">The receiver monitor factory.</param>
    /// <param name="receiverFactory">The EventStore receiver factory.</param>
    /// <param name="dataAdapter">The EventStore data adapter.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="hostEnvironmentStatistics">The host environment statistics.</param>
    public EventStoreQueueAdapter(string name,
                                  EventStoreOptions options,
                                  EventStoreReceiverOptions receiverOptions,
                                  HashRingBasedPartitionedStreamQueueMapper streamQueueMapper,
                                  Func<string, IStreamQueueCheckpointer<string>, ILoggerFactory, IEventStoreQueueCache> cacheFactory,
                                  Func<string, Task<IStreamQueueCheckpointer<string>>> checkpointerFactory,
                                  Func<EventStoreReceiverMonitorDimensions, ILoggerFactory, IQueueAdapterReceiverMonitor> receiverMonitorFactory,
                                  Func<EventStoreReceiverSettings, string, ILogger, IEventStoreReceiver> receiverFactory,
                                  IEventStoreDataAdapter dataAdapter,
                                  IServiceProvider serviceProvider,
                                  ILoggerFactory loggerFactory,
                                  IHostEnvironmentStatistics? hostEnvironmentStatistics)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(receiverOptions, nameof(receiverOptions));
        ArgumentNullException.ThrowIfNull(streamQueueMapper, nameof(streamQueueMapper));
        ArgumentNullException.ThrowIfNull(cacheFactory, nameof(cacheFactory));
        ArgumentNullException.ThrowIfNull(checkpointerFactory, nameof(checkpointerFactory));
        ArgumentNullException.ThrowIfNull(receiverMonitorFactory, nameof(receiverMonitorFactory));
        ArgumentNullException.ThrowIfNull(receiverFactory, nameof(receiverFactory));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        Name = name;
        _options = options;
        _receiverOptions = receiverOptions;
        _streamQueueMapper = streamQueueMapper;
        _cacheFactory = cacheFactory;
        _checkpointerFactory = checkpointerFactory;
        _receiverMonitorFactory = receiverMonitorFactory;
        _receiverFactory = receiverFactory;
        _dataAdapter = dataAdapter;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _hostEnvironmentStatistics = hostEnvironmentStatistics;
    }

    #region IQueueAdapter Implementation

    /// <summary>
    ///     Name of the adapter. Primarily for logging purposes
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Determines whether this is a rewindable stream adapter - supports subscribing from previous point in time.
    /// </summary>
    /// <returns>True if this is a rewindable stream adapter, false otherwise.</returns>
    public bool IsRewindable => true;

    /// <summary>
    ///     Direction of this queue adapter: Read, Write or ReadWrite.
    /// </summary>
    /// <returns>The direction in which this adapter provides data.</returns>
    public StreamProviderDirection Direction { get; protected set; } = StreamProviderDirection.ReadWrite;

    /// <summary>
    ///     Writes a set of events to the queue as a single batch associated with the provided streamId.
    /// </summary>
    /// <typeparam name="T">The queue element type.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="events">The events.</param>
    /// <param name="token">The token.</param>
    /// <param name="requestContext">The request context.</param>
    /// <returns>Task.</returns>
    public Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
    {
        var queueId = _streamQueueMapper.GetQueueForStream(streamId);
        var producer = _producers.GetOrAdd(queueId, static (qid, instance) => instance.MakeProducer(qid), this);
        var eventData = _dataAdapter.ToQueueMessage(streamId, events, token, requestContext);
        return producer.AppendAsync(eventData);
    }

    private EventStoreProducer MakeProducer(QueueId queueId)
    {
        var producerSettings = new EventStoreProducerSettings
                               {
                                   Options = _options,
                                   QueueName = _streamQueueMapper.QueueToPartition(queueId)
                               };
        return new EventStoreProducer(producerSettings, _loggerFactory.CreateLogger<EventStoreProducer>());
    }

    /// <summary>
    ///     Creates a queue receiver for the specified queueId
    /// </summary>
    /// <param name="queueId">The queue identifier.</param>
    /// <returns>The receiver.</returns>
    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        return GetOrCreateReceiver(queueId);
    }

    private EventStoreQueueAdapterReceiver GetOrCreateReceiver(QueueId queueId)
    {
        return _receivers.GetOrAdd(queueId, static (qid, instance) => instance.MakeReceiver(qid), this);
    }

    private EventStoreQueueAdapterReceiver MakeReceiver(QueueId queueId)
    {
        var receiverSettings = new EventStoreReceiverSettings
                               {
                                   Options = _options,
                                   ReceiverOptions = _receiverOptions,
                                   // TODO
                                   ConsumerGroup = "",
                                   QueueName = _streamQueueMapper.QueueToPartition(queueId)
                               };
        var receiverMonitorDimensions = new EventStoreReceiverMonitorDimensions
                                        {
                                            QueueName = receiverSettings.QueueName,
                                            Name = receiverSettings.Options.Name
                                        };
        return new EventStoreQueueAdapterReceiver(receiverSettings, _cacheFactory, _checkpointerFactory, _loggerFactory, _receiverMonitorFactory(receiverMonitorDimensions, _loggerFactory), _serviceProvider.GetRequiredService<IOptions<LoadSheddingOptions>>().Value, _hostEnvironmentStatistics, _receiverFactory);
    }

    #endregion

    #region IQueueAdapterCache Implementation

    /// <summary>
    ///     Create a cache for a given queue id.
    /// </summary>
    /// <param name="queueId">The queue id.</param>
    /// <returns>The queue cache..</returns>
    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return GetOrCreateReceiver(queueId);
    }

    #endregion

}
