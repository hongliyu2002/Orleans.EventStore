namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Base class for monitor aggregation dimensions, which is an information bag for the monitoring target.
///     Monitors can use this information bag to build its aggregation dimensions.
/// </summary>
public class EventStoreMonitorAggregationDimensions
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMonitorAggregationDimensions" /> class.
    /// </summary>
    public EventStoreMonitorAggregationDimensions()
    {
        EventStoreName = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMonitorAggregationDimensions" /> class.
    /// </summary>
    /// <param name="eventStoreName"></param>
    public EventStoreMonitorAggregationDimensions(string eventStoreName)
    {
        EventStoreName = eventStoreName;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMonitorAggregationDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    public EventStoreMonitorAggregationDimensions(EventStoreMonitorAggregationDimensions dimensions)
    {
        EventStoreName = dimensions.EventStoreName;
    }

    /// <summary>
    ///     EventStore path
    /// </summary>
    public string EventStoreName { get; set; }
}
