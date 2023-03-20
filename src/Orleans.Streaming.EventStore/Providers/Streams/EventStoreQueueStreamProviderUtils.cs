using Orleans.Configuration;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Utility functions for EventStore queue Persistent stream provider.
/// </summary>
public static class EventStoreQueueStreamProviderUtils
{
    /// <summary>
    ///     Generate default azure queue names
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="providerName"></param>
    /// <returns></returns>
    public static List<string> GenerateDefaultEventStoreQueueNames(string serviceId, string providerName)
    {
        var defaultQueueMapper = new HashRingBasedStreamQueueMapper(new HashRingStreamQueueMapperOptions(), providerName);
        return defaultQueueMapper.GetAllQueues().Select(queueId => $"{serviceId}-{queueId}").ToList();
    }
}
