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
        Name = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMonitorAggregationDimensions" /> class.
    /// </summary>
    /// <param name="name"></param>
    public EventStoreMonitorAggregationDimensions(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreMonitorAggregationDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    public EventStoreMonitorAggregationDimensions(EventStoreMonitorAggregationDimensions dimensions)
    {
        Name = dimensions.Name;
    }

    /// <summary>
    ///     EventStore name.
    /// </summary>
    public string Name { get; set; }
}
