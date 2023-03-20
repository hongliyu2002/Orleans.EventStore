using System.Globalization;
using Newtonsoft.Json;
using Orleans.Providers.Streams.Common;

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
public class EventStoreSequenceToken : EventSequenceToken, IEventStoreLocation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreSequenceToken" /> class.
    /// </summary>
    /// <remarks>
    ///     This constructor is exposed for serializer use only.
    /// </remarks>
    public EventStoreSequenceToken()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreSequenceToken" /> class.
    /// </summary>
    /// <param name="offset">A potential logical record offset in the Event Store transaction file.</param>
    /// <param name="sequenceNumber">A offset within a stream.</param>
    /// <param name="eventIndex">Index into a batch of events, if multiple events were delivered within a single EventStore message.</param>
    public EventStoreSequenceToken(string offset, long sequenceNumber, int eventIndex)
        : base(sequenceNumber, eventIndex)
    {
        Offset = offset;
    }

    /// <summary>
    ///     A potential logical record offset in the Event Store transaction file.
    /// </summary>
    [Id(0)]
    [JsonProperty]
    public string Offset { get; set; } = string.Empty;

    /// <summary>
    ///     Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    /// <filterpriority>2</filterpriority>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "EventStoreSequenceToken(Offset: {0}, SequenceNumber: {1}, EventIndex: {2})", Offset, SequenceNumber, EventIndex);
    }
}
