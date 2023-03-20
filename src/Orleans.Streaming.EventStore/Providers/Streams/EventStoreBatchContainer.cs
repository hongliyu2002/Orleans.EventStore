using EventStore.Client;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Streaming.Providers.Streams;

/// <summary>
///     Batch container that is delivers payload and stream position information for a set of events in an EventStore EventData.
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreBatchContainer : IBatchContainer
{
    /// <summary>
    /// </summary>
    /// <param name="serializer"></param>
    [GeneratedActivatorConstructor]
    internal EventStoreBatchContainer(Serializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    ///     Batch container that delivers events from cached EventStore data associated with an orleans stream
    /// </summary>
    public EventStoreBatchContainer(EventStoreMessage eventStoreMessage, Serializer serializer)
    {
        _eventStoreMessage = eventStoreMessage;
        _serializer = serializer;
        _sequenceToken = new EventStoreSequenceTokenV2(eventStoreMessage.Offset, eventStoreMessage.SequenceNumber, 0);
    }

    [JsonProperty]
    [Id(0)]
    private readonly EventStoreMessage _eventStoreMessage = null!;

    [JsonProperty]
    [Id(1)]
    private readonly EventStoreSequenceToken _sequenceToken = null!;

    /// <summary>
    ///     Stream identifier for the stream this batch is part of.
    /// </summary>
    public StreamId StreamId => _eventStoreMessage.StreamId;

    /// <summary>
    ///     Stream Sequence Token for the start of this batch.
    /// </summary>
    public StreamSequenceToken SequenceToken => _sequenceToken;

    [JsonIgnore]
    [NonSerialized]
    internal Serializer _serializer;

    /// <summary>
    ///     Payload is local cache of deserialized payloadBytes.
    ///     Should never be serialized as part of batch container.
    ///     During batch container serialization raw payloadBytes will always be used.
    /// </summary>
    [NonSerialized]
    private Body? _payload;

    internal Body Payload => _payload ??= _serializer.Deserialize<Body>(_eventStoreMessage.Payload);

    /// <inheritdoc />
    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
    {
        return Payload.Events.Cast<T>().Select((evt, index) => Tuple.Create<T, StreamSequenceToken>(evt, new EventStoreSequenceTokenV2(_sequenceToken.Offset, _sequenceToken.SequenceNumber, index)));
    }

    /// <summary>
    ///     Gives an opportunity to IBatchContainer to set any data in the RequestContext before this IBatchContainer is sent to consumers.
    ///     It can be the data that was set at the time event was generated and enqueued into the persistent provider or any other data.
    /// </summary>
    /// <returns>True if the RequestContext was indeed modified, false otherwise.</returns>
    public bool ImportRequestContext()
    {
        if (Payload.RequestContext != null)
        {
            RequestContextExtensions.Import(Payload.RequestContext);
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Put events list and its context into a EventData object
    /// </summary>
    public static EventData ToEventData<T>(Serializer serializer, StreamId streamId, IEnumerable<T> events, Dictionary<string, object> requestContext)
    {
        var payload = new Body
                      {
                          Events = events.Cast<object>().ToList(),
                          RequestContext = requestContext
                      };
        var data = new ReadOnlyMemory<byte>(serializer.SerializeToArray(payload));
        var typeName = payload.Events.Count <= 1 ? typeof(T).Name : $"{typeof(T).Name} List";
        return new EventData(Uuid.NewUuid(), typeName, data, streamId.FullKey, "application/octet-stream");
    }

    #region Internal Class

    [Serializable]
    [GenerateSerializer]
    internal class Body
    {
        [Id(0)]
        public List<object> Events { get; set; } = new();

        [Id(1)]
        public Dictionary<string, object> RequestContext { get; set; } = new();
    }

    #endregion

}
