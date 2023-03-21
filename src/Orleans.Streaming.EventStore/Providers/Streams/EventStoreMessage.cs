using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Replication of EventStore EventData And EventRecord class,
///     reconstructed from cached data CachedEventStoreMessage
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMessage" /> class.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="eventId"></param>
    /// <param name="eventType"></param>
    /// <param name="position"></param>
    /// <param name="sequenceNumber"></param>
    /// <param name="enqueueTimeUtc"></param>
    /// <param name="dequeueTimeUtc"></param>
    /// <param name="contentType"></param>
    /// <param name="data"></param>
    public EventStoreMessage(StreamId streamId, string eventId, string eventType, string position, long sequenceNumber, DateTime enqueueTimeUtc, DateTime dequeueTimeUtc, string contentType, ReadOnlyMemory<byte> data)
    {
        StreamId = streamId;
        EventId = eventId;
        EventType = eventType;
        Position = position;
        SequenceNumber = sequenceNumber;
        EnqueueTimeUtc = enqueueTimeUtc;
        DequeueTimeUtc = dequeueTimeUtc;
        ContentType = contentType;
        Data = data;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMessage" /> class.
    /// </summary>
    /// <param name="cachedMessage"></param>
    public EventStoreMessage(CachedMessage cachedMessage)
    {
        var readOffset = 0;
        StreamId = cachedMessage.StreamId;
        EventId = SegmentBuilder.ReadNextString(cachedMessage.Segment, ref readOffset);
        EventType = SegmentBuilder.ReadNextString(cachedMessage.Segment, ref readOffset);
        Position = SegmentBuilder.ReadNextString(cachedMessage.Segment, ref readOffset);
        SequenceNumber = cachedMessage.SequenceNumber;
        EnqueueTimeUtc = cachedMessage.EnqueueTimeUtc;
        DequeueTimeUtc = cachedMessage.DequeueTimeUtc;
        ContentType = SegmentBuilder.ReadNextString(cachedMessage.Segment, ref readOffset);
        Data = SegmentBuilder.ReadNextBytes(cachedMessage.Segment, ref readOffset);
    }

    /// <summary>
    ///     The stream identifier.
    /// </summary>
    [Id(0)]
    public StreamId StreamId { get; set; }

    /// <summary>
    ///     The id of the event in the stream.
    /// </summary>
    [Id(1)]
    public string EventId { get; set; }

    /// <summary>
    ///     The type name of the event.
    /// </summary>
    [Id(2)]
    public string EventType { get; set; }

    /// <summary>
    ///     Referring to a potential logical record position in the EventStore transaction file.
    /// </summary>
    [Id(3)]
    public string Position { get; set; }

    /// <summary>
    ///     The position of the event in the stream.
    /// </summary>
    [Id(4)]
    public long SequenceNumber { get; set; }

    /// <summary>
    ///     The time message was written to the message queue.
    /// </summary>
    [Id(5)]
    public DateTime EnqueueTimeUtc { get; set; }

    /// <summary>
    ///     The time this message was read from the message queue.
    /// </summary>
    [Id(6)]
    public DateTime DequeueTimeUtc { get; set; }

    /// <summary>
    ///     The contenty type of the event payload.
    ///     "application/json" or "application/octet-stream" should be used.
    /// </summary>
    [Id(7)]
    public string ContentType { get; set; }

    /// <summary>
    ///     The raw bytes representing the data of this event.
    /// </summary>
    [Id(8)]
    public ReadOnlyMemory<byte> Data { get; set; }
}
