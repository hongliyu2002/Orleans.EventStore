using EventStore.Client;

namespace Orleans.Configuration;

/// <summary>
///     EventStore settings.
/// </summary>
public class EventStoreOptions
{
    /// <summary>
    ///     The EventStore client settings.
    /// </summary>
    public EventStoreClientSettings ClientSettings { get; set; } = null!;

    /// <summary>
    ///     The user credentials that have permissions to create persistent subscription.
    /// </summary>
    public UserCredentials? Credentials { get; set; }

    /// <summary>
    ///     EventStore name for this connection, used in cache monitor.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    ///     The queue names (aka stream names) of EventStore.
    /// </summary>
    public List<string> Queues { get; set; } = new();
}
