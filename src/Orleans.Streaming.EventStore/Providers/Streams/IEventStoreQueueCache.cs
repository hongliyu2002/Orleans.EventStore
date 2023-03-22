using EventStore.Client;
using Orleans.Runtime;
using Orleans.Streams;
using StreamPosition = Orleans.Streams.StreamPosition;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Interface for a stream queue messages cache that stores EventStore EventRecord
/// </summary>
public interface IEventStoreQueueCache : IQueueFlowController, IDisposable
{
    /// <summary>
    ///     Add a list of EventStore EventRecord to the cache.
    /// </summary>
    /// <param name="queueMessages"></param>
    /// <param name="dequeueTimeUtc"></param>
    /// <returns></returns>
    List<StreamPosition> Add(List<EventRecord> queueMessages, DateTime dequeueTimeUtc);

    /// <summary>
    ///     Get a cursor into the cache to read events from a stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="sequenceToken"></param>
    /// <returns></returns>
    object GetCursor(StreamId streamId, StreamSequenceToken sequenceToken);

    /// <summary>
    ///     Try to get the next queue messages in the cache for the provided cursor.
    /// </summary>
    /// <param name="cursorObj"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    bool TryGetNextMessage(object cursorObj, out IBatchContainer container);

    /// <summary>
    ///     Add cache pressure monitor to the cache's back pressure algorithm
    /// </summary>
    /// <param name="monitor"></param>
    void AddCachePressureMonitor(ICachePressureMonitor monitor);

    /// <summary>
    ///     Send purge signal to the cache, the cache will perform a time based purge on its cached messages
    /// </summary>
    void SignalPurge();
}
