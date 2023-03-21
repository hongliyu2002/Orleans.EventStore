namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Interface for In-memory proxy stream queue grain for accessing EventStore.
/// </summary>
public interface IEventStoreQueueGrain : IGrainWithGuidKey
{
    /// <summary>
    ///     Enqueues an event.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>A <see cref="Task" /> representing the operation.</returns>
    Task Enqueue(EventStoreMessage message);

    /// <summary>
    ///     Dequeues up to <paramref name="maxCount" /> events.
    /// </summary>
    /// <param name="maxCount">
    ///     The maximum number of events to dequeue.
    /// </param>
    /// <returns>A <see cref="Task" /> representing the operation.</returns>
    Task<List<EventStoreMessage>> Dequeue(int maxCount);
}
