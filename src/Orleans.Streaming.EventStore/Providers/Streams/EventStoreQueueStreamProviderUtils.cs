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
    /// <param name="totalQueueCount"></param>
    /// <returns></returns>
    public static List<string> GenerateDefaultQueueNames(string serviceId, string providerName, int totalQueueCount)
    {
        var options = new HashRingStreamQueueMapperOptions();
        options.TotalQueueCount = totalQueueCount;
        var defaultQueueMapper = new HashRingBasedStreamQueueMapper(options, providerName);
        return defaultQueueMapper.GetAllQueues().Select(queueId => $"{serviceId}/streaming/{queueId}").ToList();
    }
}
