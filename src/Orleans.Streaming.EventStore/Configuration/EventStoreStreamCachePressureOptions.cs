namespace Orleans.Configuration;

/// <summary>
/// </summary>
public class EventStoreStreamCachePressureOptions
{
    /// <summary>
    /// </summary>
    public const double AveragingCachePressureMonitoringOff = 1.0;

    /// <summary>
    /// </summary>
    public const double DefaultAveragingCachePressureMonitoringThreshold = 1.0 / 3.0;

    /// <summary>
    ///     Slow consuming pressure monitor config.
    /// </summary>
    public double? SlowConsumingMonitorFlowControlThreshold { get; set; }

    /// <summary>
    ///     Slow consuming monitor pressure windowsize.
    /// </summary>
    public TimeSpan? SlowConsumingMonitorPressureWindowSize { get; set; }

    /// <summary>
    ///     AveragingCachePressureMonitorFlowControlThreshold, AveragingCachePressureMonitor is turn on by default.
    ///     User can turn it off by setting this value to null
    /// </summary>
    public double? AveragingCachePressureMonitorFlowControlThreshold { get; set; } = DefaultAveragingCachePressureMonitoringThreshold;
}
