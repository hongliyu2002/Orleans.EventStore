using Orleans.Providers.Streams.Common;

namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Default EventStore receiver monitor that tracks metrics using loggers PKI support.
/// </summary>
public class DefaultEventStoreReceiverMonitor : DefaultQueueAdapterReceiverMonitor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultEventStoreCacheMonitor" /> class.
    /// </summary>
    /// <param name="dimensions">Aggregation Dimension bag for EventStoreReceiverMonitor</param>
    public DefaultEventStoreReceiverMonitor(EventStoreReceiverMonitorDimensions dimensions)
        : base(new KeyValuePair<string, object>[]
               {
                   new("Path", dimensions.EventStorePath),
                   new("Stream", dimensions.EventStoreStream)
               })
    {
    }
}
