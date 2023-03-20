using EventStore.Client;

namespace Orleans.Configuration;

/// <summary>
///     EventStore streaming storage options.
/// </summary>
public class EventStoreQueueOptions
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
    public int QueueBufferSize { get; set; } = 32;

    /// <summary>
    ///     The stream names (aka queue names) on the EventStore server.
    /// </summary>
    public List<string> QueueNames { get; set; } = new();
}
