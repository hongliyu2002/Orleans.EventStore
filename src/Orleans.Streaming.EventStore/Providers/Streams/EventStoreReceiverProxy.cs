using System.Collections.Concurrent;
using System.Diagnostics;
using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Orleans.Streaming.EventStoreStorage;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Subscribe from EventStore persistent subscriptions and receive data from internal queue.
/// </summary>
public class EventStoreReceiverProxy : IEventStoreReceiver
{
    private readonly EventStoreSubscriptionSettings _settings;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<EventRecord, bool> _hashSet = new();
    private readonly ConcurrentQueue<EventRecord> _queue = new();

    private EventStorePersistentSubscriptionsClient? _subscriptionClient;
    private PersistentSubscription? _subscription;
    private bool _initialized;

    /// <summary>
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="position"></param>
    /// <param name="logger"></param>
    public EventStoreReceiverProxy(EventStoreSubscriptionSettings settings, IPosition position, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(settings.ClientSettings, nameof(settings.ClientSettings));
        ArgumentNullException.ThrowIfNull(settings.SubscriptionSettings, nameof(settings.SubscriptionSettings));
        ArgumentException.ThrowIfNullOrEmpty(settings.StreamName, nameof(settings.StreamName));
        ArgumentException.ThrowIfNullOrEmpty(settings.ConsumerGroup, nameof(settings.ConsumerGroup));
        ArgumentException.ThrowIfNullOrEmpty(settings.StreamName, nameof(settings.StreamName));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _settings = settings;
        var original = _settings.SubscriptionSettings;
        _settings.SubscriptionSettings = new PersistentSubscriptionSettings(original.ResolveLinkTos,
                                                                            position,
                                                                            original.ExtraStatistics,
                                                                            original.MessageTimeout,
                                                                            original.MaxRetryCount,
                                                                            original.LiveBufferSize,
                                                                            original.ReadBatchSize,
                                                                            original.HistoryBufferSize,
                                                                            original.CheckPointAfter,
                                                                            original.CheckPointLowerBound,
                                                                            original.CheckPointUpperBound,
                                                                            original.MaxSubscriberCount,
                                                                            original.ConsumerStrategyName);
        _logger = logger;
    }

    /// <summary>
    ///     Start to create client and subscribe from EventStore persistent subscriptions.
    /// </summary>
    public async Task InitAsync()
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreReceiverProxy for stream {StreamName} is initializing.", _settings.StreamName);
            }
            _subscriptionClient = new EventStorePersistentSubscriptionsClient(_settings.ClientSettings);
            try
            {
                await _subscriptionClient.CreateAsync(_settings.StreamName, _settings.ConsumerGroup, _settings.SubscriptionSettings, null, _settings.Credentials).ConfigureAwait(false);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
            {
                await _subscriptionClient.DeleteAsync(_settings.StreamName, _settings.ConsumerGroup, null, _settings.Credentials).ConfigureAwait(false);
                await _subscriptionClient.CreateAsync(_settings.StreamName, _settings.ConsumerGroup, _settings.SubscriptionSettings, null, _settings.Credentials).ConfigureAwait(false);
            }
            _subscription = await _subscriptionClient.SubscribeToStreamAsync(_settings.StreamName, _settings.ConsumerGroup, OnEventAppeared, OnSubscriptionDropped, _settings.Credentials, _settings.PrefetchCount).ConfigureAwait(false);
            _initialized = true;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                timer.Stop();
                _logger.LogDebug("InitAsync: StreamName={StreamName}, initialized in {ElapsedMilliseconds} ms", _settings.StreamName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "InitAsync: StreamName={StreamName}, errored in {ElapsedMilliseconds} ms.", _settings.StreamName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to init EventStore persistent subscriptions client, {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Clean up.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_initialized == false || _subscription == null || _subscriptionClient == null)
        {
            return;
        }
        try
        {
            _queue.Clear();
            _hashSet.Clear();
            _subscription.Dispose();
            await _subscriptionClient.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloseAsync: StreamName={StreamName}", _settings.StreamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to close EventStore persistent subscriptions client, {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _subscription = null;
            _subscriptionClient = null;
            _initialized = false;
        }
    }

    /// <summary>
    ///     Asking for more messages from internal queue.
    /// </summary>
    /// <param name="maxCount">Max amount of message which should be delivered in this request</param>
    /// <returns></returns>
    public List<EventRecord> Receive(int maxCount)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Receiving events from internal queue: {StreamName}", _settings.StreamName);
        }
        if (_initialized == false)
        {
            return new List<EventRecord>();
        }
        try
        {
            var eventRecords = new List<EventRecord>();
            for (var i = 0; i < maxCount; i++)
            {
                if (!_queue.TryDequeue(out var eventRecord))
                {
                    break;
                }
                eventRecords.Add(eventRecord);
                _hashSet.TryRemove(eventRecord, out _);
            }
            return eventRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive events from internal queue for the stream {StreamName}.", _settings.StreamName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to receive events from internal queue for the stream {_settings.StreamName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    #region Internal Queue Handlers

    private Task OnEventAppeared(PersistentSubscription subscription, ResolvedEvent resolvedEvent, int? retryCount, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("EventAppeared from subscription: {Subscription} for the stream: {StreamName}", subscription.SubscriptionId, _settings.StreamName);
        }
        try
        {
            if (_hashSet.TryAdd(resolvedEvent.Event, true))
            {
                _queue.Enqueue(resolvedEvent.Event);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle event from subscription for the stream {StreamName}.", _settings.StreamName);
        }
        return Task.CompletedTask;
    }

    private async void OnSubscriptionDropped(PersistentSubscription subscription, SubscriptionDroppedReason reason, Exception? exception)
    {
        try
        {
            switch (reason)
            {
                case SubscriptionDroppedReason.ServerError:
                    _logger.LogWarning("SubscriptionDropped from subscription: {Subscription} for the stream: {StreamName} with server error: {Error}", subscription.SubscriptionId, _settings.StreamName, exception?.Message);
                    await ResubscribeAsync(1000);
                    break;
                case SubscriptionDroppedReason.SubscriberError:
                    _logger.LogWarning("SubscriptionDropped from subscription: {Subscription} for the stream: {StreamName} with client error: {Error}", subscription.SubscriptionId, _settings.StreamName, exception?.Message);
                    await ResubscribeAsync(10);
                    break;
                case SubscriptionDroppedReason.Disposed:
                    _logger.LogInformation("SubscriptionDropped from subscription: {Subscription} for the stream: {StreamName} successfully", subscription.SubscriptionId, _settings.StreamName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle subscription dropped for the stream {StreamName}.", _settings.StreamName);
        }
    }

    private async Task ResubscribeAsync(int retryCount)
    {
        if (_initialized == false || _subscriptionClient == null)
        {
            return;
        }
        for (var i = 0; i < retryCount; i++)
        {
            try
            {
                _subscription = await _subscriptionClient.SubscribeToStreamAsync(_settings.StreamName, _settings.ConsumerGroup, OnEventAppeared, OnSubscriptionDropped, _settings.Credentials, _settings.PrefetchCount);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Successfully reconnected to the stream {StreamName}.", _settings.StreamName);
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to the stream {StreamName}. Retrying... Attempt: {Attempt}", _settings.StreamName, i + 1);
                await Task.Delay(TimeSpan.FromSeconds(i));
            }
        }
        _logger.LogError("Failed to reconnect to the stream {StreamName} after {Retries} attempts. Aborting...", _settings.StreamName, retryCount);
    }

    #endregion

}
