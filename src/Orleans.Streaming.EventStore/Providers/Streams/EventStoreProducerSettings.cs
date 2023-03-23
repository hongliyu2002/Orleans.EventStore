using Orleans.Configuration;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     EventStore client settings for stream producer.
/// </summary>
public class EventStoreProducerSettings
{
    /// <summary>
    ///     EventStore options.
    /// </summary>
    public EventStoreOptions Options { get; set; } = null!;

    /// <summary>
    ///     The queue name (aks stream name) from EventStore.
    /// </summary>
    public string QueueName { get; set; } = null!;
}
