using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Factory responsible for creating a message cache for an EventStore partition.
/// </summary>
public interface IEventStoreQueueCacheFactory
{
    /// <summary>
    ///     Function used to create a IEventStoreQueueCache
    /// </summary>
    IEventStoreQueueCache CreateCache(string partition, IStreamQueueCheckpointer<string> checkpointer, ILoggerFactory loggerFactory);
}
