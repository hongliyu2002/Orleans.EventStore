namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Aggregation dimensions for cache monitor used in EventStore stream provider ecosystem
/// </summary>
public class EventStoreCacheMonitorDimensions : EventStoreReceiverMonitorDimensions
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreCacheMonitorDimensions" /> class.
    /// </summary>
    public EventStoreCacheMonitorDimensions()
    {
        BlockPoolId = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreCacheMonitorDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    /// <param name="queue"></param>
    /// <param name="blockPoolId"></param>
    public EventStoreCacheMonitorDimensions(EventStoreMonitorAggregationDimensions dimensions, string queue, string blockPoolId)
        : base(dimensions, queue)
    {
        BlockPoolId = blockPoolId;
    }

    /// <summary>
    ///     Block pool this cache belongs to.
    /// </summary>
    public string BlockPoolId { get; set; }

}
