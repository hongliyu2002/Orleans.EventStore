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
    ///     The name of a EventStore stream.
    /// </summary>
    public string QueueName { get; set; } = null!;
}
