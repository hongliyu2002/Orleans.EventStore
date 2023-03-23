using Orleans.Providers.Streams.Common;

namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Default cache monitor for EventStore streaming provider ecosystem
/// </summary>
public class DefaultEventStoreCacheMonitor : DefaultCacheMonitor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultEventStoreCacheMonitor" /> class.
    /// </summary>
    /// <param name="dimensions">Aggregation Dimension bag for EventStoreCacheMonitor</param>
    public DefaultEventStoreCacheMonitor(EventStoreCacheMonitorDimensions dimensions)
        : base(new KeyValuePair<string, object>[]
               {
                   new("Path", dimensions.EventStorePath),
                   new("Stream", dimensions.EventStoreStream)
               })
    {
    }
}
