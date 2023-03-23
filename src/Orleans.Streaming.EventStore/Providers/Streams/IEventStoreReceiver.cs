using EventStore.Client;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Abstraction on EventStoreReceiver class, used to configure EventStoreReceiver class in EventStoreQueueAdapterReceiver,
///     also used to configure EventStoreGeneratorReceiver in EventStoreQueueAdapterReceiver for testing purpose
/// </summary>
public interface IEventStoreReceiver
{
    /// <summary>
    ///     Start to create client and subscribe from EventStore persistent subscriptions.
    /// </summary>
    Task InitAsync();

    /// <summary>
    ///     Clean up.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    ///     Asking for more messages from internal queue.
    /// </summary>
    /// <param name="maxCount">Max amount of message which should be delivered</param>
    /// <returns></returns>
    List<EventRecord> Receive(int maxCount);

    /// <summary>
    ///     Acknowledge that a message has completed processing (this will tell the server it has been processed).
    /// </summary>
    /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
    /// <param name="eventIds">
    ///     The <see cref="T:EventStore.Client.Uuid" /> of the <see cref="T:EventStore.Client.ResolvedEvent" />s to acknowledge.
    ///     There should not be more than 2000 to ack at a time.
    /// </param>
    Task AckAsync(params Uuid[] eventIds);

    /// <summary>
    ///     Acknowledge that a message has failed processing (this will tell the server it has not been processed).
    /// </summary>
    /// <param name="action">The <see cref="T:EventStore.Client.PersistentSubscriptionNakEventAction" /> to take.</param>
    /// <param name="reason">A reason given.</param>
    /// <param name="eventIds">
    ///     The <see cref="T:EventStore.Client.Uuid" /> of the <see cref="T:EventStore.Client.ResolvedEvent" />s to nak.
    ///     There should not be more than 2000 to nak at a time.
    /// </param>
    Task NackAsync(PersistentSubscriptionNakEventAction action, string reason, params Uuid[] eventIds);
}
