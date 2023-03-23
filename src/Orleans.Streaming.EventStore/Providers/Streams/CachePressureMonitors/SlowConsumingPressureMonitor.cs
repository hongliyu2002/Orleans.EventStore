using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Pressure monitor which is in favor of the slow consumer in the cache
/// </summary>
public class SlowConsumingPressureMonitor : ICachePressureMonitor
{
    /// <summary>
    ///     Default flow control threshold
    /// </summary>
    public const double DefaultFlowControlThreshold = 0.5;

    /// <summary>
    ///     Default pressure window size.
    /// </summary>
    public static TimeSpan DefaultPressureWindowSize = TimeSpan.FromMinutes(1);

    private readonly ILogger _logger;
    private double _biggestPressureInCurrentWindow;
    private bool _isUnderPressure;
    private ICacheMonitor? _monitor;

    private DateTime _nextCheckedTime;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SlowConsumingPressureMonitor" /> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="monitor"></param>
    public SlowConsumingPressureMonitor(ILogger logger, ICacheMonitor? monitor = null)
        : this(DefaultFlowControlThreshold, DefaultPressureWindowSize, logger, monitor)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SlowConsumingPressureMonitor" /> class.
    /// </summary>
    /// <param name="pressureWindowSize"></param>
    /// <param name="logger"></param>
    /// <param name="monitor"></param>
    public SlowConsumingPressureMonitor(TimeSpan pressureWindowSize, ILogger logger, ICacheMonitor? monitor = null)
        : this(DefaultFlowControlThreshold, pressureWindowSize, logger, monitor)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SlowConsumingPressureMonitor" /> class.
    /// </summary>
    /// <param name="flowControlThreshold"></param>
    /// <param name="logger"></param>
    /// <param name="monitor"></param>
    public SlowConsumingPressureMonitor(double flowControlThreshold, ILogger logger, ICacheMonitor? monitor = null)
        : this(flowControlThreshold, DefaultPressureWindowSize, logger, monitor)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SlowConsumingPressureMonitor" /> class.
    /// </summary>
    /// <param name="flowControlThreshold"></param>
    /// <param name="pressureWindowSzie"></param>
    /// <param name="logger"></param>
    /// <param name="monitor"></param>
    public SlowConsumingPressureMonitor(double flowControlThreshold, TimeSpan pressureWindowSzie, ILogger logger, ICacheMonitor? monitor = null)
    {
        FlowControlThreshold = flowControlThreshold;
        PressureWindowSize = pressureWindowSzie;
        _logger = logger;
        _nextCheckedTime = DateTime.MinValue;
        _biggestPressureInCurrentWindow = 0;
        _isUnderPressure = false;
        _monitor = monitor;
    }

    /// <summary>
    ///     Pressure window size.
    /// </summary>
    public TimeSpan PressureWindowSize { get; set; }

    /// <summary>
    ///     Flow control threshold.
    /// </summary>
    public double FlowControlThreshold { get; set; }

    /// <summary>
    ///     Cache monitor which is used to report cache related metrics.
    /// </summary>
    public ICacheMonitor CacheMonitor
    {
        set => _monitor = value;
    }

    /// <summary>
    ///     Record cache pressure contribution to the monitor.
    /// </summary>
    /// <param name="cachePressureContribution"></param>
    public void RecordCachePressureContribution(double cachePressureContribution)
    {
        if (cachePressureContribution > _biggestPressureInCurrentWindow)
        {
            _biggestPressureInCurrentWindow = cachePressureContribution;
        }
    }

    /// <summary>
    ///     Determine if the monitor is under pressure.
    /// </summary>
    /// <param name="utcNow"></param>
    /// <returns></returns>
    public bool IsUnderPressure(DateTime utcNow)
    {
        //if any pressure contribution in current period is bigger than flowControlThreshold
        //we see the cache is under pressure
        var underPressure = _biggestPressureInCurrentWindow > FlowControlThreshold;
        if (underPressure && !_isUnderPressure)
        {
            //if under pressure, extend the _nextCheckedTime, make sure isUnderPressure is true for a whole window  
            _isUnderPressure = underPressure;
            _nextCheckedTime = utcNow + PressureWindowSize;
            _monitor?.TrackCachePressureMonitorStatusChange(GetType().Name, underPressure, null, _biggestPressureInCurrentWindow, FlowControlThreshold);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Ingesting messages too fast. Throttling message reading. BiggestPressureInCurrentPeriod: {BiggestPressureInCurrentWindow}, Threshold: {FlowControlThreshold}", _biggestPressureInCurrentWindow, FlowControlThreshold);
            }
            _biggestPressureInCurrentWindow = 0;
        }
        if (_nextCheckedTime < utcNow)
        {
            //at the end of each check period, reset biggestPressureInCurrentPeriod
            _nextCheckedTime = utcNow + PressureWindowSize;
            _biggestPressureInCurrentWindow = 0;
            //if at the end of the window, pressure clears out, log
            if (_isUnderPressure && !underPressure)
            {
                _monitor?.TrackCachePressureMonitorStatusChange(GetType().Name, underPressure, null, _biggestPressureInCurrentWindow, FlowControlThreshold);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Message ingestion is healthy. BiggestPressureInCurrentPeriod: {BiggestPressureInCurrentWindow}, Threshold: {FlowControlThreshold}", _biggestPressureInCurrentWindow, FlowControlThreshold);
                }
            }
            _isUnderPressure = underPressure;
        }
        return _isUnderPressure;
    }
}
