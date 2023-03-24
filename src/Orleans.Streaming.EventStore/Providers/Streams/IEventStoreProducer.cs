using EventStore.Client;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Abstraction on EventStoreProducer class, used to configure EventStoreProducer class in EventStoreQueueAdapter,
///     also used to configure EventStoreGeneratorProducer in EventStoreQueueAdapter for testing purpose
/// </summary>
public interface IEventStoreProducer
{
    /// <summary>
    ///     Start to create client and subscribe from EventStore persistent subscriptions.
    /// </summary>
    void Init();

    /// <summary>
    ///     Clean up.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    ///     Appends events asynchronously to a stream.
    /// </summary>
    /// <param name="events"></param>
    /// <returns></returns>
    Task AppendAsync(params EventData[] events);
}
