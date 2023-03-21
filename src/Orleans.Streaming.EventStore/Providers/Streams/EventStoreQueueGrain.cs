namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///  </summary>
public class EventStoreQueueGrain : Grain, IEventStoreQueueGrain
{
    private readonly Queue<EventStoreMessage> _eventQueue = new();
    
    /// <inheritdoc />
    public async Task Enqueue(EventStoreMessage message)
    {
    }

    /// <inheritdoc />
    public async Task<List<EventStoreMessage>> Dequeue(int maxCount)
    {
        return null;
    }
}
