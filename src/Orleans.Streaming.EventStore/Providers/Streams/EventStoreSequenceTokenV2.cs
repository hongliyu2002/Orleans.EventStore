namespace Orleans.Streaming.Providers.Streams;

/// <summary>
///     EventStore messages consist of a batch of application layer events, so EventStore tokens contain three pieces of information.
///     Offset - this is a unique value per stream that is used to start reading from this message in the stream.
///     SequencePosition - EventStore sequence numbers are unique ordered message IDs for messages within a stream.
///     The SequencePosition is required for uniqueness and ordering of EventStore messages within a stream.
///     event Index - Since each EventStore message may contain more than one application layer event, this value
///     indicates which application layer event this token is for, within an EventStore message.  It is required for uniqueness
///     and ordering of application layer events within an EventStore message.
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreSequenceTokenV2 : EventStoreSequenceToken
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreSequenceTokenV2" /> class.
    /// </summary>
    /// <remarks>
    ///     This constructor is exposed for serializer use only.
    /// </remarks>
    public EventStoreSequenceTokenV2()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreSequenceTokenV2" /> class.
    /// </summary>
    /// <param name="offset">A potential logical record offset in the Event Store transaction file.</param>
    /// <param name="sequenceNumber">A offset within a stream.</param>
    /// <param name="eventIndex">Index into a batch of events, if multiple events were delivered within a single EventStore message.</param>
    public EventStoreSequenceTokenV2(string offset, long sequenceNumber, int eventIndex)
        : base(offset, sequenceNumber, eventIndex)
    {
    }
}
