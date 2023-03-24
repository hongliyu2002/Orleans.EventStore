using System.Diagnostics;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Streaming.EventStoreStorage;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Append events data to EventStore stream.
/// </summary>
public class EventStoreProducer : IEventStoreProducer
{
    private readonly EventStoreProducerSettings _settings;
    private readonly ILogger _logger;

    private EventStoreClient? _client;
    private bool _initialized;

    /// <summary>
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public EventStoreProducer(EventStoreProducerSettings settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(settings.Options.ClientSettings, nameof(settings.Options.ClientSettings));
        ArgumentException.ThrowIfNullOrEmpty(settings.QueueName, nameof(settings.QueueName));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    ///     Start to create client and subscribe from EventStore persistent subscriptions.
    /// </summary>
    public void Init()
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreProducer for stream {QueueName} is initializing.", _settings.QueueName);
            }
            _client = new EventStoreClient(_settings.Options.ClientSettings);
            _initialized = true;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                timer.Stop();
                _logger.LogDebug("InitAsync: QueueName={QueueName}, initialized in {ElapsedMilliseconds} ms", _settings.QueueName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "InitAsync: QueueName={QueueName}, errored in {ElapsedMilliseconds} ms.", _settings.QueueName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to init EventStore persistent subscriptions client, {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Clean up.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_initialized == false || _client == null)
        {
            return;
        }
        try
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloseAsync: QueueName={QueueName}", _settings.QueueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to close EventStore persistent subscriptions client, {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _client = null;
            _initialized = false;
        }
    }

    /// <summary>
    ///     Appends events asynchronously to a stream.
    /// </summary>
    /// <param name="events"></param>
    /// <returns></returns>
    public async Task AppendAsync(params EventData[] events)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Appending events to stream: {QueueName}", _settings.QueueName);
        }
        if (_initialized == false || _client == null || events == null)
        {
            return;
        }
        try
        {
            await _client.AppendToStreamAsync(_settings.QueueName, StreamState.Any, events, null, null, _settings.Options.Credentials).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append events for the the stream {QueueName}.", _settings.QueueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to append events for stream {_settings.QueueName}. {ex.GetType()}: {ex.Message}"));
        }
    }
}
