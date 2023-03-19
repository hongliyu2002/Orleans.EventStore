using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

public class EventStoreQueueAdapterFactory: IQueueAdapterFactory
{

    /// <inheritdoc />
    public async Task<IQueueAdapter> CreateAdapter()
    {
        return null;
    }

    /// <inheritdoc />
    public IQueueAdapterCache GetQueueAdapterCache()
    {
        return null;
    }

    /// <inheritdoc />
    public IStreamQueueMapper GetStreamQueueMapper()
    {
        return null;
    }

    /// <inheritdoc />
    public async Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return null;
    }
}
