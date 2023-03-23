using Orleans.Configuration;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     EventStore subscription settings for stream receiver.
/// </summary>
public class EventStoreReceiverSettings
{
    /// <summary>
    ///     EventStore options.
    /// </summary>
    public EventStoreOptions Options { get; set; } = null!;

    /// <summary>
    ///     EventStore receiver options.
    /// </summary>
    public EventStoreReceiverOptions ReceiverOptions { get; set; } = null!;

    /// <summary>
    ///     Consumer group name.
    /// </summary>
    public string ConsumerGroup { get; set; } = null!;

    /// <summary>
    ///     The name of a EventStore stream.
    /// </summary>
    public string QueueName { get; set; } = null!;
}
