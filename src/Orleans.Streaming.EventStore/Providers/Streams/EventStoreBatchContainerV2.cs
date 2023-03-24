using System.Text;
using EventStore.Client;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Each queue message is allowed to be a heterogeneous, ordered set of events.
///     <see cref="IBatchContainer" /> contains these events and allows users to query the batch for a specific type of event.
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreBatchContainerV2 : IBatchContainer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBatchContainerV2" /> class.
    /// </summary>
    [GeneratedActivatorConstructor]
    public EventStoreBatchContainerV2(Serializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer, nameof(serializer));
        Serializer = serializer;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBatchContainerV2" /> class.
    ///     Batch container that delivers events from cached EventStore data associated with an orleans stream
    /// </summary>
    /// <param name="eventStoreMessage"></param>
    /// <param name="serializer"></param>
    public EventStoreBatchContainerV2(EventStoreMessage eventStoreMessage, Serializer serializer)
        : this(serializer)
    {
        ArgumentNullException.ThrowIfNull(eventStoreMessage, nameof(eventStoreMessage));
        EventStoreMessage = eventStoreMessage;
        Token = new EventStoreSequenceTokenV2(eventStoreMessage.Position, eventStoreMessage.SequenceNumber, 0);
    }

    [JsonProperty]
    [Id(0)]
    private EventStoreMessage EventStoreMessage { get; } = null!;

    [JsonIgnore]
    [field: NonSerialized]
    private Serializer Serializer { get; set; }

    /// <summary>
    ///     Ges the stream identifier for the stream this batch is part of.
    /// </summary>
    public StreamId StreamId => EventStoreMessage.StreamId;

    /// <summary>
    ///     Ges the stream sequence token for the start of this batch.
    /// </summary>
    [JsonProperty]
    [Id(1)]
    private EventStoreSequenceTokenV2 Token { get; set; } = new();

    /// <summary>
    ///     Ges the stream sequence token for the start of this batch.
    /// </summary>
    public StreamSequenceToken SequenceToken => Token;

    // Payload is local cache of deserialized payloadBytes.
    // Should never be serialized as part of batch container.
    // During batch container serialization raw payloadBytes will always be used.
    [NonSerialized]
    private MessageBody? _payload;

    private MessageBody Payload => _payload ??= Serializer.Deserialize<MessageBody>(EventStoreMessage.Data);

    #region IBatchContainer Implementation

    /// <summary>
    ///     Gets events of a specific type from the batch.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return Payload.Events.Cast<T>().Select((evt, index) => Tuple.Create<T, StreamSequenceToken>(evt, new EventStoreSequenceTokenV2(Token.Position, Token.SequenceNumber, index)));
    }

    /// <summary>
    ///     Gives an opportunity to <see cref="IBatchContainer" /> to set any data in the <see cref="Runtime.RequestContext" /> before this <see cref="IBatchContainer" /> is sent to consumers.
    ///     It can be the data that was set at the time event was generated and enqueued into the persistent provider or any other data.
    /// </summary>
    /// <returns><see langword="true" /> if the <see cref="Runtime.RequestContext" /> was indeed modified, <see langword="false" /> otherwise.</returns>
    public bool ImportRequestContext()
    {
        if (Payload.RequestContext != null)
        {
            RequestContextExtensions.Import(Payload.RequestContext);
            return true;
        }
        return false;
    }

    #endregion

    #region Static Method

    /// <summary>
    ///     Put events list and its context into a EventData object
    /// </summary>
    public static EventData ToEventData<T>(Serializer serializer, StreamId streamId, IEnumerable<T> events, Dictionary<string, object> requestContext)
    {
        ArgumentNullException.ThrowIfNull(events);
        var payload = new MessageBody(events.Cast<object>().ToList(), requestContext);
        var payloadBuffer = serializer.SerializeToArray(payload);
        var eventData = new EventData(Uuid.NewUuid(), typeof(T).Name, new ReadOnlyMemory<byte>(payloadBuffer), new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(streamId.ToString())), "application/octet-stream");
        return eventData;
    }

    #endregion

    #region Internal Class

    /// <summary>
    ///     Message body.
    /// </summary>
    [Serializable]
    [GenerateSerializer]
    internal sealed class MessageBody
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageBody" /> class.
        /// </summary>
        /// <param name="events">Events that are part of this message.</param>
        /// <param name="requestContext">Context in which this message was sent.</param>
        public MessageBody(IEnumerable<object> events, Dictionary<string, object> requestContext)
        {
            ArgumentNullException.ThrowIfNull(events, nameof(events));
            Events = events.ToList();
            RequestContext = requestContext;
        }

        /// <summary>
        ///     Gets the events in the message.
        /// </summary>
        [Id(0)]
        public List<object> Events { get; }

        /// <summary>
        ///     Gets the message request context.
        /// </summary>
        [Id(1)]
        public Dictionary<string, object> RequestContext { get; }
    }

    #endregion

}
