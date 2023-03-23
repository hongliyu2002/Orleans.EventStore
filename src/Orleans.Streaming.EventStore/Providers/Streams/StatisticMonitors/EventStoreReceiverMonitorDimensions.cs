namespace Orleans.Providers.Streams.EventStore.StatisticMonitors;

/// <summary>
///     Aggregation dimensions for EventStoreReceiverMonitor
/// </summary>
public class EventStoreReceiverMonitorDimensions : EventStoreMonitorAggregationDimensions
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreReceiverMonitorDimensions" /> class.
    /// </summary>
    public EventStoreReceiverMonitorDimensions()
    {
        EventStoreStream = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreReceiverMonitorDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    /// <param name="esStream"></param>
    public EventStoreReceiverMonitorDimensions(EventStoreMonitorAggregationDimensions dimensions, string esStream)
        : base(dimensions)
    {
        EventStoreStream = esStream;
    }

    /// <summary>
    ///     EventStore stream
    /// </summary>
    public string EventStoreStream { get; set; }
}
