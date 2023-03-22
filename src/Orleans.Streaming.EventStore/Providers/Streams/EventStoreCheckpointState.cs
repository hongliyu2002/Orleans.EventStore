using Orleans.Streaming.EventStoreStorage;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
/// </summary>
[Serializable]
[GenerateSerializer]
public class EventStoreCheckpointState : IEventStoreState
{
    /// <summary>
    /// </summary>
    public EventStoreCheckpointState()
    {
        Position = global::EventStore.Client.Position.Start.ToString();
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
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="streamProviderName"></param>
    /// <param name="partition"></param>
    /// <returns></returns>
    public static string GetStreamName(string serviceId, string streamProviderName, string partition)
    {
        return $"{serviceId}/checkpoints/streaming/{streamProviderName}/{partition}";
    }
}
