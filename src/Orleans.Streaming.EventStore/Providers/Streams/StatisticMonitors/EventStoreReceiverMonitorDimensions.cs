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
        EventStoreQueueName = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreReceiverMonitorDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    /// <param name="eventStoreQueueName"></param>
    public EventStoreReceiverMonitorDimensions(EventStoreMonitorAggregationDimensions dimensions, string eventStoreQueueName)
        : base(dimensions)
    {
        EventStoreQueueName = eventStoreQueueName;
    }

    /// <summary>
    ///     EventStore stream
    /// </summary>
    public string EventStoreQueueName { get; set; }
}
