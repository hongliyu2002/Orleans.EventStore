using EventStore.Client;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;
using StreamPosition = Orleans.Streams.StreamPosition;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Converts event data to and from queue message。
///     Data adapter that uses types that support custom serializers (like json).
/// </summary>
public class EventStoreQueueDataAdapter : IEventStoreDataAdapter
{
    private readonly Serializer _serializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreQueueDataAdapter" /> class.
    /// </summary>
    /// <param name="serializer"></param>
    public EventStoreQueueDataAdapter(Serializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    ///     Converts a cached message to a batch container for delivery
    /// </summary>
    /// <param name="cachedMessage"></param>
    /// <returns></returns>
    public virtual IBatchContainer GetBatchContainer(ref CachedMessage cachedMessage)
    {
        var evenStoreMessage = new EventStoreMessage(cachedMessage);
        return GetBatchContainer(evenStoreMessage);
    }

    /// <summary>
    ///     Convert an EventStoreMessage to a batch container
    /// </summary>
    /// <param name="eventStoreMessage"></param>
    /// <returns></returns>
    protected virtual IBatchContainer GetBatchContainer(EventStoreMessage eventStoreMessage)
    {
        return new EventStoreBatchContainer(eventStoreMessage, _serializer);
    }

    /// <summary>
    ///     Gets the stream sequence token from a cached message.
    /// </summary>
    /// <param name="cachedMessage"></param>
    /// <returns></returns>
    public virtual StreamSequenceToken GetSequenceToken(ref CachedMessage cachedMessage)
    {
        return new EventStoreSequenceToken("", cachedMessage.SequenceNumber, 0);
    }

    /// <summary>
    ///     Creates a cloud queue message from stream event data.
    /// </summary>
    /// <typeparam name="T">The stream event type.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="events">The events.</param>
    /// <param name="sequenceToken">The sequence sequenceToken.</param>
    /// <param name="requestContext">The request context.</param>
    /// <returns>A new queue message.</returns>
    public virtual EventData ToQueueMessage<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken sequenceToken, Dictionary<string, object> requestContext)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));
        return EventStoreBatchContainer.ToEventData(_serializer, streamId, events, requestContext);
    }

    /// <summary>
    ///     Creates a CachedMessage from a cloud queue message
    /// </summary>
    /// <returns>The message batch.</returns>
    public virtual CachedMessage FromQueueMessage(StreamPosition position, EventRecord queueMessage, DateTime dequeueTime, Func<int, ArraySegment<byte>> getSegment)
    {
        return new CachedMessage
               {
                   StreamId = position.StreamId,
                   SequenceNumber = queueMessage.EventNumber.ToInt64(),
                   EventIndex = position.SequenceToken.EventIndex,
                   EnqueueTimeUtc = queueMessage.Created,
                   DequeueTimeUtc = dequeueTime,
                   Segment = EncodeMessageIntoSegment(queueMessage, getSegment)
               };
    }

    /// <summary>
    ///     Get orleans stream position from the event message.
    /// </summary>
    /// <param name="queueMessage"></param>
    /// <returns></returns>
    public virtual StreamPosition GetStreamPosition(EventRecord queueMessage)
    {
        var streamId = GetStreamId(queueMessage);
        var sequenceToken = new EventStoreSequenceToken(queueMessage.Position.CommitPosition.ToString(), queueMessage.EventNumber.ToInt64(), 0);
        return new StreamPosition(streamId, sequenceToken);
    }

    /// <summary>
    ///     Get position from cached message.
    ///     Left to derived class, as only it knows how to get this from the cached message.
    /// </summary>
    public virtual string GetPosition(CachedMessage cachedMessage)
    {
        var readOffset = 0;
        return SegmentBuilder.ReadNextString(cachedMessage.Segment, ref readOffset);
    }

    /// <summary>
    ///     Get the <see cref="IStreamIdentity" /> for an event message.
    /// </summary>
    /// <param name="queueMessage">The event message.</param>
    /// <returns>The stream identity.</returns>
    public virtual StreamId GetStreamId(EventRecord queueMessage)
    {
        return StreamId.Parse(queueMessage.Metadata.Span);
    }

    /// <summary>
    ///     Placed object message payload into a segment.
    /// </summary>
    /// <param name="queueMessage"></param>
    /// <param name="getSegment"></param>
    /// <returns></returns>
    protected virtual ArraySegment<byte> EncodeMessageIntoSegment(EventRecord queueMessage, Func<int, ArraySegment<byte>> getSegment)
    {
        var position = queueMessage.Position.CommitPosition.ToString();
        var eventId = queueMessage.EventId.ToString();
        var eventType = queueMessage.EventType;
        var data = queueMessage.Data.Span;
        // get total size.
        var size = SegmentBuilder.CalculateAppendSize(position) + SegmentBuilder.CalculateAppendSize(eventId) + SegmentBuilder.CalculateAppendSize(eventType) + SegmentBuilder.CalculateAppendSize(data);
        // get segment
        var segment = getSegment(size);
        // encode
        var writeOffset = 0;
        SegmentBuilder.Append(segment, ref writeOffset, position);
        SegmentBuilder.Append(segment, ref writeOffset, eventId);
        SegmentBuilder.Append(segment, ref writeOffset, eventType);
        SegmentBuilder.Append(segment, ref writeOffset, data);
        return segment;
    }
}
