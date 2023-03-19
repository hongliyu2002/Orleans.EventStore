using Newtonsoft.Json;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Each queue message is allowed to be a heterogeneous, ordered set of events.
///     <see cref="IBatchContainer" /> contains these events and allows users to query the batch for a specific type of event.
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreBatchContainer : IBatchContainer
{
    /// <summary>
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="events"></param>
    /// <param name="requestContext"></param>
    public EventStoreBatchContainer(StreamId streamId, List<object> events, Dictionary<string, object> requestContext)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));
        StreamId = streamId;
        Events = events;
        RequestContext = requestContext;
        SequenceToken = new EventSequenceToken();
    }

    /// <summary>
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="events"></param>
    /// <param name="requestContext"></param>
    /// <param name="sequenceToken"></param>
    [JsonConstructor]
    public EventStoreBatchContainer(StreamId streamId, List<object> events, Dictionary<string, object> requestContext, StreamSequenceToken sequenceToken)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));
        StreamId = streamId;
        Events = events;
        RequestContext = requestContext;
        SequenceToken = sequenceToken == null ? new EventSequenceToken() : sequenceToken as EventSequenceToken ?? new EventSequenceToken(sequenceToken.SequenceNumber, sequenceToken.EventIndex);
    }

    /// <summary>
    ///     Ges the stream sequence token for the start of this batch.
    /// </summary>
    [JsonProperty]
    [Id(0)]
    public StreamSequenceToken SequenceToken { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty]
    [Id(1)]
    private List<object> Events { get; }

    /// <summary>
    /// </summary>
    [JsonProperty]
    [Id(2)]
    private Dictionary<string, object> RequestContext { get; }

    /// <summary>
    ///     Ges the stream identifier for the stream this batch is part of.
    /// </summary>
    [Id(3)]
    public StreamId StreamId { get; }

    /// <summary>
    ///     Gets events of a specific type from the batch.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return Events.OfType<T>().Select((x, i) => Tuple.Create<T, StreamSequenceToken>(x, new EventSequenceToken(SequenceToken.SequenceNumber, i)));
    }

    /// <summary>
    ///     Gives an opportunity to <see cref="IBatchContainer" /> to set any data in the <see cref="Runtime.RequestContext" /> before this <see cref="IBatchContainer" /> is sent to consumers.
    ///     It can be the data that was set at the time event was generated and enqueued into the persistent provider or any other data.
    /// </summary>
    /// <returns><see langword="true" /> if the <see cref="Runtime.RequestContext" /> was indeed modified, <see langword="false" /> otherwise.</returns>
    public bool ImportRequestContext()
    {
        if (RequestContext == null)
        {
            return false;
        }
        RequestContextExtensions.Import(RequestContext);
        return true;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"[EventStoreBatchContainer:Stream={StreamId},#Items={Events.Count}]";
    }
}
