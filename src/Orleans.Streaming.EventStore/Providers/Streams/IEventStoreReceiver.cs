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
}
