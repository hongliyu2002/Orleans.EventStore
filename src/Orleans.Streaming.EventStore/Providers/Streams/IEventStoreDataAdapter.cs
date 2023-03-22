using EventStore.Client;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;
using StreamPosition = Orleans.Streams.StreamPosition;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
/// </summary>
public interface IEventStoreDataAdapter : IQueueDataAdapter<EventData>, ICacheDataAdapter
{
    /// <summary>
    /// </summary>
    /// <param name="position"></param>
    /// <param name="queueMessage"></param>
    /// <param name="dequeueTime"></param>
    /// <param name="getSegment"></param>
    /// <returns></returns>
    CachedMessage FromQueueMessage(StreamPosition position, EventRecord queueMessage, DateTime dequeueTime, Func<int, ArraySegment<byte>> getSegment);

    /// <summary>
    ///     Get orleans stream position from the event message.
    /// </summary>
    /// <param name="queueMessage"></param>
    /// <returns></returns>
    StreamPosition GetStreamPosition(EventRecord queueMessage);

    /// <summary>
    ///     Get EventStore event position from cached message.
    /// </summary>
    /// <param name="cachedMessage"></param>
    /// <returns></returns>
    string GetPosition(CachedMessage cachedMessage);

    /// <summary>
    ///     Get the <see cref="IStreamIdentity" /> for an event message.
    /// </summary>
    /// <param name="queueMessage">The event message.</param>
    /// <returns>The stream identity.</returns>
    StreamId GetStreamId(EventRecord queueMessage);
}
