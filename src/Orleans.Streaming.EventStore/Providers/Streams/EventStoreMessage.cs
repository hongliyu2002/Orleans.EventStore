using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Streaming.Providers.Streams;

/// <summary>
///     Replication of EventStore EventData class, reconstructed from cached data CachedEventStoreMessage
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMessage" /> class.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="offset"></param>
    /// <param name="sequenceNumber"></param>
    /// <param name="enqueueTimeUtc"></param>
    /// <param name="dequeueTimeUtc"></param>
    /// <param name="payload"></param>
    public EventStoreMessage(StreamId streamId, string offset, long sequenceNumber, DateTime enqueueTimeUtc, DateTime dequeueTimeUtc, ReadOnlyMemory<byte> payload)
    {
        StreamId = streamId;
        Offset = offset;
        SequenceNumber = sequenceNumber;
        EnqueueTimeUtc = enqueueTimeUtc;
        DequeueTimeUtc = dequeueTimeUtc;
        Payload = payload;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMessage" /> class.
    /// </summary>
    /// <param name="cachedMessage"></param>
    public EventStoreMessage(CachedMessage cachedMessage)
    {
        var readOffset = 0;
        StreamId = cachedMessage.StreamId;
        Offset = SegmentBuilder.ReadNextString(cachedMessage.Segment, ref readOffset);
        SequenceNumber = cachedMessage.SequenceNumber;
        EnqueueTimeUtc = cachedMessage.EnqueueTimeUtc;
        DequeueTimeUtc = cachedMessage.DequeueTimeUtc;
        Payload = SegmentBuilder.ReadNextBytes(cachedMessage.Segment, ref readOffset);
    }

    /// <summary>
    ///     Stream identifier
    /// </summary>
    [Id(0)]
    public StreamId StreamId { get; }

    /// <summary>
    ///     A potential logical record position in the Event Store transaction file.
    /// </summary>
    [Id(1)]
    public string Offset { get; }

    /// <summary>
    ///     A position within a stream.
    /// </summary>
    [Id(2)]
    public long SequenceNumber { get; }

    /// <summary>
    ///     Time event was written to EventStore
    /// </summary>
    [Id(3)]
    public DateTime EnqueueTimeUtc { get; }

    /// <summary>
    ///     Time event was read from EventStore and added to cache
    /// </summary>
    [Id(4)]
    public DateTime DequeueTimeUtc { get; }

    /// <summary>
    ///     Binary event data
    /// </summary>
    [Id(5)]
    public ReadOnlyMemory<byte> Payload { get; }
}
