using EventStore.Client;

namespace Orleans.Streaming.Configuration;

/// <summary>
///     EventStore streaming storage options.
/// </summary>
public class EventStoreStorageOptions
{
    /// <summary>
    ///     The EventStore client settings.
    /// </summary>
    [Redact]
    public EventStoreClientSettings ClientSettings { get; set; } = null!;

    /// <summary>
    ///     The user credentials that have permissions to create persistent subscription and append events.
    /// </summary>
    [Redact]
    public UserCredentials? Credentials { get; set; }

    /// <summary>
    ///     The EventStore persistent subscription settings.
    /// </summary>
    public PersistentSubscriptionSettings SubscriptionSettings { get; set; } = new();

    /// <summary>
    ///     The buffer size used in persistent subscription client queue.
    /// </summary>
    public int SubscriptionQueueBufferSize { get; set; } = 32;
}
