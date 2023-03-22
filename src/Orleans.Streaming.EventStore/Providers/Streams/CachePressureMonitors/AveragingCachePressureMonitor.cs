using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Cache pressure monitor whose back pressure algorithm is based on averaging pressure value
///     over all pressure contribution
/// </summary>
public class AveragingCachePressureMonitor : ICachePressureMonitor
{
    private static readonly TimeSpan s_CheckPeriod = TimeSpan.FromSeconds(2);

    private readonly double _flowControlThreshold;
    private readonly ILogger _logger;
    private bool _isUnderPressure;
    private ICacheMonitor? _monitor;

    private DateTime _nextCheckedTime;

    private double _accumulatedCachePressure;
    private double _cachePressureContributionCount;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="monitor"></param>
    public AveragingCachePressureMonitor(ILogger logger, ICacheMonitor? monitor = null)
        : this(EventStoreStreamCachePressureOptions.DefaultAveragingCachePressureMonitoringThreshold, logger, monitor)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="flowControlThreshold"></param>
    /// <param name="logger"></param>
    /// <param name="monitor"></param>
    public AveragingCachePressureMonitor(double flowControlThreshold, ILogger logger, ICacheMonitor? monitor = null)
    {
        _flowControlThreshold = flowControlThreshold;
        _logger = logger;
        _nextCheckedTime = DateTime.MinValue;
        _isUnderPressure = false;
        _monitor = monitor;
    }

    /// <summary>
    ///     Cache monitor which is used to report cache related metrics
    /// </summary>
    public ICacheMonitor CacheMonitor
    {
        set => _monitor = value;
    }

    /// <inheritdoc />
    public void RecordCachePressureContribution(double cachePressureContribution)
    {
        // Weight unhealthy contributions thrice as much as healthy ones.
        // This is a crude compensation for the fact that healthy consumers wil consume more often than unhealthy ones.
        var weight = cachePressureContribution < _flowControlThreshold ? 1.0 : 3.0;
        _accumulatedCachePressure += cachePressureContribution * weight;
        _cachePressureContributionCount += weight;
    }

    /// <inheritdoc />
    public bool IsUnderPressure(DateTime utcNow)
    {
        if (_nextCheckedTime < utcNow)
        {
            CalculatePressure();
            _nextCheckedTime = utcNow + s_CheckPeriod;
        }
        return _isUnderPressure;
    }

    private void CalculatePressure()
    {
        // if we don't have any contributions, don't change status
        if (_cachePressureContributionCount < 0.5)
        {
            // after 5 checks with no contributions, check anyway
            _cachePressureContributionCount += 0.1;
            return;
        }
        var pressure = _accumulatedCachePressure / _cachePressureContributionCount;
        var wasUnderPressure = _isUnderPressure;
        _isUnderPressure = pressure > _flowControlThreshold;
        // If we changed state, log
        if (_isUnderPressure != wasUnderPressure)
        {
            _monitor?.TrackCachePressureMonitorStatusChange(GetType().Name, _isUnderPressure, _cachePressureContributionCount, pressure, _flowControlThreshold);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(_isUnderPressure ?
                                     $"Ingesting messages too fast. Throttling message reading. AccumulatedCachePressure: {_accumulatedCachePressure}, Contributions: {_cachePressureContributionCount}, AverageCachePressure: {pressure}, Threshold: {_flowControlThreshold}" :
                                     $"Message ingestion is healthy. AccumulatedCachePressure: {_accumulatedCachePressure}, Contributions: {_cachePressureContributionCount}, AverageCachePressure: {pressure}, Threshold: {_flowControlThreshold}");
            }
        }
        _cachePressureContributionCount = 0.0;
        _accumulatedCachePressure = 0.0;
    }
}
