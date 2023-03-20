using Newtonsoft.Json;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Each queue message is allowed to be a heterogeneous, ordered set of events.
///     <see cref="IBatchContainer" /> contains these events and allows users to query the batch for a specific type of event.
///     Second version of EventStoreBatchContainer.  This version supports external serializers (like json)
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreBatchContainerV2 : IBatchContainer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBatchContainerV2" /> class.
    /// </summary>
    public EventStoreBatchContainerV2()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBatchContainerV2" /> class.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="events"></param>
    /// <param name="requestContext"></param>
    public EventStoreBatchContainerV2(StreamId streamId, List<object> events, Dictionary<string, object> requestContext)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));
        StreamId = streamId;
        Events = events;
        RequestContext = requestContext;
        Token = new EventSequenceTokenV2();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBatchContainerV2" /> class.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="events"></param>
    /// <param name="requestContext"></param>
    /// <param name="sequenceToken"></param>
    [JsonConstructor]
    public EventStoreBatchContainerV2(StreamId streamId, List<object> events, Dictionary<string, object> requestContext, EventSequenceTokenV2 sequenceToken)
        : this(streamId, events, requestContext)
    {
        Token = sequenceToken;
    }

    /// <summary>
    ///     Ges the stream sequence token for the start of this batch.
    /// </summary>
    [Id(0)]
    [JsonProperty]
    internal EventSequenceTokenV2 Token { get; set; } = new();

    /// <summary>
    ///     Ges the stream sequence token for the start of this batch.
    /// </summary>
    [JsonIgnore]
    public StreamSequenceToken SequenceToken => Token;

    /// <summary>
    /// </summary>
    [Id(1)]
    [JsonProperty]
    private List<object> Events { get; } = new();

    /// <summary>
    /// </summary>
    [Id(2)]
    [JsonProperty]
    private Dictionary<string, object> RequestContext { get; } = new();

    /// <summary>
    ///     Ges the stream identifier for the stream this batch is part of.
    /// </summary>
    [Id(3)]
    [JsonProperty]
    public StreamId StreamId { get; }

    /// <summary>
    ///     Gets events of a specific type from the batch.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return Events.OfType<T>().Select((evt, index) => Tuple.Create<T, StreamSequenceToken>(evt, Token.CreateSequenceTokenForEvent(index)));
    }

    /// <summary>
    ///     Gives an opportunity to <see cref="IBatchContainer" /> to set any data in the <see cref="Runtime.RequestContext" /> before this <see cref="IBatchContainer" /> is sent to consumers.
    ///     It can be the data that was set at the time event was generated and enqueued into the persistent provider or any other data.
    /// </summary>
    /// <returns><see langword="true" /> if the <see cref="Runtime.RequestContext" /> was indeed modified, <see langword="false" /> otherwise.</returns>
    public bool ImportRequestContext()
    {
        if (RequestContext != null)
        {
            RequestContextExtensions.Import(RequestContext);
            return true;
        }
        return false;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"[EventStoreBatchContainerV2:Stream={StreamId},#Items={Events.Count}]";
    }
}
