using EventStore.Client;

namespace Orleans.Configuration;

/// <summary>
/// </summary>
public class EventStoreReceiverOptions
{
    /// <summary>
    ///     The EventStore persistent subscription settings.
    /// </summary>
    public PersistentSubscriptionSettings SubscriptionSettings { get; set; } = null!;

    /// <summary>
    ///     Optional parameter that configures the receiver prefetch count.
    /// </summary>
    public int PrefetchCount { get; set; } = 32;

    /// <summary>
    ///     In cases where no checkpoint is found,
    ///     this indicates if service should read from the most recent data, or from the beginning of a stream.
    /// </summary>
    public bool StartFromNow { get; set; } = true;
}
