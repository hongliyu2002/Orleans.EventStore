using System.Collections.Concurrent;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

internal sealed class EventStoreQueueAdapter : IQueueAdapter
{
    private readonly EventStoreQueueOptions _queueOptions;
    private readonly IOptions<ClusterOptions> _clusterOptions;
    private readonly HashRingBasedPartitionedStreamQueueMapper _streamQueueMapper;
    private readonly IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> _dataAdapter;
    private readonly ILoggerFactory _loggerFactory;

    private readonly ConcurrentDictionary<QueueId, EventStoreQueueStorage> _queues = new();

    public EventStoreQueueAdapter(string name,
                                  EventStoreQueueOptions queueOptions,
                                  IOptions<ClusterOptions> clusterOptions,
                                  HashRingBasedPartitionedStreamQueueMapper streamQueueMapper,
                                  IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> dataAdapter,
                                  ILoggerFactory loggerFactory)
    {
        Name = name;
        _queueOptions = queueOptions;
        _clusterOptions = clusterOptions;
        _streamQueueMapper = streamQueueMapper;
        _dataAdapter = dataAdapter;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsRewindable => true;

    /// <inheritdoc />
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

    /// <inheritdoc />
    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        var queueName = _streamQueueMapper.QueueToPartition(queueId);
        return EventStoreQueueAdapterReceiver.Create(queueName, _queueOptions, _clusterOptions, _dataAdapter, _loggerFactory);
    }

    /// <inheritdoc />
    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken sequenceToken, Dictionary<string, object> requestContext)
    {
        var queueId = _streamQueueMapper.GetQueueForStream(streamId);
        if (!_queues.TryGetValue(queueId, out var queueStorage))
        {
            var newQueueStorage = new EventStoreQueueStorage(_streamQueueMapper.QueueToPartition(queueId), _clusterOptions.Value.ServiceId, _queueOptions, _loggerFactory.CreateLogger<EventStoreQueueStorage>());
            await newQueueStorage.Init();
            queueStorage = _queues.GetOrAdd(queueId, newQueueStorage);
        }
        var messageData = _dataAdapter.ToQueueMessage(streamId, events, sequenceToken, requestContext);
        await queueStorage.AppendAsync(new EventData(Uuid.NewUuid(), typeof(T).Name, messageData));
    }

}
