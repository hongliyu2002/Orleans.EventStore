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
public class EventStoreReceiver : IEventStoreReceiver
{
    private readonly EventStoreReceiverSettings _settings;
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
    public EventStoreReceiver(EventStoreReceiverSettings settings, IPosition position, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(settings.Options.ClientSettings, nameof(settings.Options.ClientSettings));
        ArgumentNullException.ThrowIfNull(settings.ReceiverOptions.SubscriptionSettings, nameof(settings.ReceiverOptions.SubscriptionSettings));
        ArgumentException.ThrowIfNullOrEmpty(settings.ConsumerGroup, nameof(settings.ConsumerGroup));
        ArgumentException.ThrowIfNullOrEmpty(settings.QueueName, nameof(settings.QueueName));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _settings = settings;
        var originSettings = _settings.ReceiverOptions.SubscriptionSettings;
        _settings.ReceiverOptions.SubscriptionSettings = new PersistentSubscriptionSettings(originSettings.ResolveLinkTos,
                                                                                            position,
                                                                                            originSettings.ExtraStatistics,
                                                                                            originSettings.MessageTimeout,
                                                                                            originSettings.MaxRetryCount,
                                                                                            originSettings.LiveBufferSize,
                                                                                            originSettings.ReadBatchSize,
                                                                                            originSettings.HistoryBufferSize,
                                                                                            originSettings.CheckPointAfter,
                                                                                            originSettings.CheckPointLowerBound,
                                                                                            originSettings.CheckPointUpperBound,
                                                                                            originSettings.MaxSubscriberCount,
                                                                                            originSettings.ConsumerStrategyName);
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
                _logger.LogDebug("EventStoreReceiver for stream {QueueName} is initializing.", _settings.QueueName);
            }
            _subscriptionClient = new EventStorePersistentSubscriptionsClient(_settings.Options.ClientSettings);
            try
            {
                await _subscriptionClient.CreateAsync(_settings.QueueName, _settings.ConsumerGroup, _settings.ReceiverOptions.SubscriptionSettings, null, _settings.Options.Credentials).ConfigureAwait(false);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
            {
                await _subscriptionClient.DeleteAsync(_settings.QueueName, _settings.ConsumerGroup, null, _settings.Options.Credentials).ConfigureAwait(false);
                await _subscriptionClient.CreateAsync(_settings.QueueName, _settings.ConsumerGroup, _settings.ReceiverOptions.SubscriptionSettings, null, _settings.Options.Credentials).ConfigureAwait(false);
            }
            _subscription = await _subscriptionClient.SubscribeToStreamAsync(_settings.QueueName, _settings.ConsumerGroup, OnEventAppeared, OnSubscriptionDropped, _settings.Options.Credentials, _settings.ReceiverOptions.PrefetchCount).ConfigureAwait(false);
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
            _logger.LogError(ex, "CloseAsync: QueueName={QueueName}", _settings.QueueName);
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
            _logger.LogTrace("Receiving events from internal queue: {QueueName}", _settings.QueueName);
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
            _logger.LogError(ex, "Failed to receive events from internal queue for stream {QueueName}.", _settings.QueueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to receive events from internal queue for stream {_settings.QueueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Acknowledge that a message has completed processing (this will tell the server it has been processed).
    /// </summary>
    /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
    /// <param name="eventIds">
    ///     The <see cref="T:EventStore.Client.Uuid" /> of the <see cref="T:EventStore.Client.ResolvedEvent" />s to acknowledge.
    ///     There should not be more than 2000 to ack at a time.
    /// </param>
    public async Task AckAsync(params Uuid[] eventIds)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Acknowledging events to stream: {QueueName}", _settings.QueueName);
        }
        if (_initialized == false || _subscription == null)
        {
            return;
        }
        try
        {
            await _subscription.Ack(eventIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge events for stream {QueueName}.", _settings.QueueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to acknowledge events for stream {_settings.QueueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Acknowledge that a message has failed processing (this will tell the server it has not been processed).
    /// </summary>
    /// <param name="action">The <see cref="T:EventStore.Client.PersistentSubscriptionNakEventAction" /> to take.</param>
    /// <param name="reason">A reason given.</param>
    /// <param name="eventIds">
    ///     The <see cref="T:EventStore.Client.Uuid" /> of the <see cref="T:EventStore.Client.ResolvedEvent" />s to nak.
    ///     There should not be more than 2000 to nak at a time.
    /// </param>
    public async Task NackAsync(PersistentSubscriptionNakEventAction action, string reason, params Uuid[] eventIds)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Nacknowledging events to stream: {QueueName}", _settings.QueueName);
        }
        if (_initialized == false || _subscription == null)
        {
            return;
        }
        try
        {
            await _subscription.Nack(action, reason, eventIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to nacknowledge events for stream {QueueName}.", _settings.QueueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to unacknowledge events for stream {_settings.QueueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    #region Internal Queue Handlers

    private Task OnEventAppeared(PersistentSubscription subscription, ResolvedEvent resolvedEvent, int? retryCount, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("EventAppeared from subscription: {Subscription} for stream: {QueueName}", subscription.SubscriptionId, _settings.QueueName);
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
            _logger.LogError(ex, "Failed to handle event from subscription for stream {QueueName}.", _settings.QueueName);
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
                    _logger.LogWarning("SubscriptionDropped from subscription: {Subscription} for stream: {QueueName} with server error: {Error}", subscription.SubscriptionId, _settings.QueueName, exception?.Message);
                    await ResubscribeAsync(1000);
                    break;
                case SubscriptionDroppedReason.SubscriberError:
                    _logger.LogWarning("SubscriptionDropped from subscription: {Subscription} for stream: {QueueName} with client error: {Error}", subscription.SubscriptionId, _settings.QueueName, exception?.Message);
                    await ResubscribeAsync(10);
                    break;
                case SubscriptionDroppedReason.Disposed:
                    _logger.LogInformation("SubscriptionDropped from subscription: {Subscription} for stream: {QueueName} successfully", subscription.SubscriptionId, _settings.QueueName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle subscription dropped for stream {QueueName}.", _settings.QueueName);
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
                _subscription = await _subscriptionClient.SubscribeToStreamAsync(_settings.QueueName, _settings.ConsumerGroup, OnEventAppeared, OnSubscriptionDropped, _settings.Options.Credentials, _settings.ReceiverOptions.PrefetchCount);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Successfully reconnected to the stream {QueueName}.", _settings.QueueName);
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to the stream {QueueName}. Retrying... Attempt: {Attempt}", _settings.QueueName, i + 1);
                await Task.Delay(TimeSpan.FromSeconds(i));
            }
        }
        _logger.LogError("Failed to reconnect to the stream {QueueName} after {Retries} attempts. Aborting...", _settings.QueueName, retryCount);
    }

    #endregion

}
