namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Aggregation dimensions for block pool monitor used in EventStore stream provider ecosystem
/// </summary>
public class EventStoreBlockPoolMonitorDimensions : EventStoreMonitorAggregationDimensions
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBlockPoolMonitorDimensions" /> class.
    /// </summary>
    public EventStoreBlockPoolMonitorDimensions()
    {
        BlockPoolId = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreBlockPoolMonitorDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    /// <param name="blockPoolId"></param>
    public EventStoreBlockPoolMonitorDimensions(EventStoreMonitorAggregationDimensions dimensions, string blockPoolId)
        : base(dimensions)
    {
        BlockPoolId = blockPoolId;
    }

    /// <summary>
    ///     Block pool Id.
    /// </summary>
    public string BlockPoolId { get; set; }
}
