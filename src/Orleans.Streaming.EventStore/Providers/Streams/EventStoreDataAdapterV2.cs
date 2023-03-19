using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Converts event data to and from queue message。
///     Data adapter that uses types that support custom serializers (like json).
/// </summary>
[SerializationCallbacks(typeof(OnDeserializedCallbacks))]
public class EventStoreDataAdapterV2 : IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>, IOnDeserialized
{
    private Serializer<EventStoreBatchContainerV2> _serializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreDataAdapterV2" /> class.
    /// </summary>
    /// <param name="serializer"></param>
    public EventStoreDataAdapterV2(Serializer serializer)
    {
        _serializer = serializer.GetSerializer<EventStoreBatchContainerV2>();
    }

    /// <summary>
    ///     Creates a cloud queue message to stream event data.
    /// </summary>
    /// <typeparam name="T">The stream event type.</typeparam>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="events">The events.</param>
    /// <param name="sequenceToken">The sequence sequenceToken.</param>
    /// <param name="requestContext">The request context.</param>
    /// <returns>A new queue message.</returns>
    public ReadOnlyMemory<byte> ToQueueMessage<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken sequenceToken, Dictionary<string, object> requestContext)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));
        var batchContainer = new EventStoreBatchContainerV2(streamId, events.Cast<object>().ToList(), requestContext, sequenceToken);
        var queueMessageBytes = _serializer.SerializeToArray(batchContainer);
        return new ReadOnlyMemory<byte>(queueMessageBytes);
    }

    /// <summary>
    ///     Creates a batch container from a cloud queue message
    /// </summary>
    /// <param name="queueMessage">The queue message.</param>
    /// <param name="sequenceNumber">The sequence identifier.</param>
    /// <returns>The message batch.</returns>
    public IBatchContainer FromQueueMessage(ReadOnlyMemory<byte> queueMessage, long sequenceNumber)
    {
        var batchContainer = _serializer.Deserialize(queueMessage);
        batchContainer.SequenceToken = new EventSequenceTokenV2(sequenceNumber);
        return batchContainer;
    }

    /// <inheritdoc />
    public void OnDeserialized(DeserializationContext context)
    {
        _serializer = context.ServiceProvider.GetRequiredService<Serializer<EventStoreBatchContainerV2>>();
    }
}
