using Orleans.Providers.Streams.Common;

namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Default monitor for Object pool used by EventStoreStreamProvider
/// </summary>
public class DefaultEventStoreBlockPoolMonitor : DefaultBlockPoolMonitor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultEventStoreBlockPoolMonitor" /> class.
    /// </summary>
    /// <param name="dimensions">Aggregation Dimension bag for EventStoreBlockPoolMonitor</param>
    public DefaultEventStoreBlockPoolMonitor(EventStoreBlockPoolMonitorDimensions dimensions)
        : base(new KeyValuePair<string, object>[]
               {
                   new("Path", dimensions.Name),
                   new("ObjectPoolId", dimensions.BlockPoolId)
               })
    {
    }
}
