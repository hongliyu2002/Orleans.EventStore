using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

internal sealed class EventStoreQueueAdapter : IQueueAdapter
{
    public EventStoreQueueAdapter(string name)
    {
        Name = name;
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
        return null;
    }
    
    /// <inheritdoc />
    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
    {
    }

}
