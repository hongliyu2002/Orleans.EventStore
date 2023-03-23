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
        QueueName = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreReceiverMonitorDimensions" /> class.
    /// </summary>
    /// <param name="dimensions"></param>
    /// <param name="queue"></param>
    public EventStoreReceiverMonitorDimensions(EventStoreMonitorAggregationDimensions dimensions, string queue)
        : base(dimensions)
    {
        QueueName = queue;
    }

    /// <summary>
    ///     EventStore stream name (Aka queue name)
    /// </summary>
    public string QueueName { get; set; }
}
