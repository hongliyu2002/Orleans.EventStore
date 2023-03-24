using Orleans.Streaming.EventStoreStorage;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Represents the state of a checkpoint for an EventStore stream.
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreCheckpointState : IEventStoreState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreCheckpointState" /> class.
    /// </summary>
    public EventStoreCheckpointState()
    {
        Position = string.Empty;
        ETag = string.Empty;
    }

    /// <summary>
    ///     Referring to a potential logical record position in the EventStore transaction file.
    /// </summary>
    [Id(0)]
    public string Position { get; set; }

    /// <summary>
    ///     The ETag value for the state.
    /// </summary>
    [Id(1)]
    public string ETag { get; set; }

    /// <summary>
    ///     Gets the checkpoint stream name based on the specified parameters.
    /// </summary>
    /// <param name="serviceId">The service identifier.</param>
    /// <param name="streamProviderName">Name of the stream provider.</param>
    /// <param name="queue">The queue.</param>
    /// <returns>The checkpoint stream name.</returns>
    public static string GetStreamName(string serviceId, string streamProviderName, string queue)
    {
        return $"{queue}/checkpoints/{serviceId}/{streamProviderName}";
    }
}
