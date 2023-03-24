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
public class EventStorePersistentSubscriptionReceiver : IEventStoreReceiver
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
    public EventStorePersistentSubscriptionReceiver(EventStoreReceiverSettings settings, string position, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(settings.Options, nameof(settings.Options));
        ArgumentNullException.ThrowIfNull(settings.Options.ClientSettings, nameof(settings.Options.ClientSettings));
        ArgumentNullException.ThrowIfNull(settings.ReceiverOptions, nameof(settings.ReceiverOptions));
        ArgumentNullException.ThrowIfNull(settings.ReceiverOptions.SubscriptionSettings, nameof(settings.ReceiverOptions.SubscriptionSettings));
        ArgumentException.ThrowIfNullOrEmpty(settings.ConsumerGroup, nameof(settings.ConsumerGroup));
        ArgumentException.ThrowIfNullOrEmpty(settings.QueueName, nameof(settings.QueueName));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _settings = settings;
        var origin = _settings.ReceiverOptions.SubscriptionSettings;
        _settings.ReceiverOptions.SubscriptionSettings = new PersistentSubscriptionSettings(origin.ResolveLinkTos, GetEventPosition(), origin.ExtraStatistics, origin.MessageTimeout, origin.MaxRetryCount, origin.LiveBufferSize, origin.ReadBatchSize, origin.HistoryBufferSize, origin.CheckPointAfter, origin.CheckPointLowerBound, origin.CheckPointUpperBound, origin.MaxSubscriberCount, origin.ConsumerStrategyName);
        _logger = logger;
        StreamPosition GetEventPosition()
        {
            // If we have a position, read from position
            if (position.TryToStreamPosition(out var streamPosition))
            {
                logger.LogInformation("Starting to read from EventStore queue {0}-{1} at position {2}", settings.Options.Name, settings.QueueName, position);
            }
            // else, if configured to start from now, start reading from most recent 
            else if (settings.ReceiverOptions.StartFromNow)
            {
                logger.LogInformation("Starting to read latest messages from EventStore queue {0}-{1}.", settings.Options.Name, settings.QueueName);
                streamPosition = StreamPosition.End;
            }
            // else, start reading from begining of the queue
            else
            {
                logger.LogInformation("Starting to read messages from begining of EventStore queue {0}-{1}.", settings.Options.Name, settings.QueueName);
                streamPosition = StreamPosition.Start;
            }
            return streamPosition;
        }
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
                _logger.LogDebug("EventStorePersistentSubscriptionReceiver for stream {QueueName} is initializing.", _settings.QueueName);
            }
            _subscriptionClient = new EventStorePersistentSubscriptionsClient(_settings.Options.ClientSettings);
            try
            {
                await _subscriptionClient.CreateToStreamAsync(_settings.QueueName, _settings.ConsumerGroup, _settings.ReceiverOptions.SubscriptionSettings, null, _settings.Options.Credentials).ConfigureAwait(false);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
            {
                await _subscriptionClient.DeleteToStreamAsync(_settings.QueueName, _settings.ConsumerGroup, null, _settings.Options.Credentials).ConfigureAwait(false);
                await _subscriptionClient.CreateToStreamAsync(_settings.QueueName, _settings.ConsumerGroup, _settings.ReceiverOptions.SubscriptionSettings, null, _settings.Options.Credentials).ConfigureAwait(false);
                // await _subscriptionClient.UpdateToStreamAsync(_settings.QueueName, _settings.ConsumerGroup, _settings.ReceiverOptions.SubscriptionSettings, null, _settings.Options.Credentials).ConfigureAwait(false);
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
        if (_initialized == false || _subscription == null)
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
            if (eventRecords.Count > 0)
            {
                _subscription.Ack(eventRecords.Select(record => record.EventId)).Ignore();
            }
            return eventRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive events from internal queue for stream {QueueName}.", _settings.QueueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to receive events from internal queue for stream {_settings.QueueName}. {ex.GetType()}: {ex.Message}"));
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
            // Check duplication
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
