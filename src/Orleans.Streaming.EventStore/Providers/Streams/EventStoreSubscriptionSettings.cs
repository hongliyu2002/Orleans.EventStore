using EventStore.Client;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     EventStore stream subscription settings
/// </summary>
public class EventStoreSubscriptionSettings
{
    /// <summary>
    ///     The EventStore client settings.
    /// </summary>
    public EventStoreClientSettings ClientSettings { get; set; } = null!;

    /// <summary>
    ///     The EventStore persistent subscription settings.
    /// </summary>
    public PersistentSubscriptionSettings SubscriptionSettings { get; set; } = null!;

    /// <summary>
    ///     The user credentials that have permissions to create persistent subscription.
    /// </summary>
    public UserCredentials? Credentials { get; set; }

    /// <summary>
    ///     The name of a EventStore stream.
    /// </summary>
    public string StreamName { get; set; } = null!;

    /// <summary>
    ///     Consumer group name.
    /// </summary>
    public string ConsumerGroup { get; set; } = null!;

    /// <summary>
    ///     Optional parameter that configures the receiver prefetch count.
    /// </summary>
    public int PrefetchCount { get; set; } = 10;
}
