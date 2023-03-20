using System.Collections.Concurrent;
using System.Diagnostics;
using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Storage;
using Orleans.Streaming.Providers;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     EventStore-based queue storage provider based on persistent subscription feature.
/// </summary>
public class EventStoreQueueStorage
{
    private readonly string _queueName;
    private readonly string _groupName;
    private readonly EventStoreQueueOptions _queueOptions;
    private readonly ILogger<EventStoreQueueStorage> _logger;

    private EventStoreClient? _client;
    private EventStorePersistentSubscriptionsClient? _subscriptionClient;
    private PersistentSubscription? _subscription;

    private readonly ConcurrentQueue<ResolvedEvent> _eventQueue = new();
    private readonly ResolvedEventsPool _eventsPool = new();

    private bool _initialized;

    /// <summary>
    ///     Creates a new instance of the <see cref="EventStoreQueueStorage" /> type.
    /// </summary>
    /// <param name="queueName">Name of the queue to be connected to.</param>
    /// <param name="groupName"></param>
    /// <param name="queueOptions"></param>
    /// <param name="logger"></param>
    public EventStoreQueueStorage(string queueName, string groupName, EventStoreQueueOptions queueOptions, ILogger<EventStoreQueueStorage> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(queueName, nameof(queueName));
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(groupName));
        ArgumentNullException.ThrowIfNull(queueOptions, nameof(queueOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        queueName = SanitizeQueueName(queueName);
        ValidateQueueName(queueName);
        _queueName = queueName;
        _groupName = groupName;
        _queueOptions = queueOptions;
        _logger = logger;
    }

    #region Lifecycle

    /// <summary>
    ///     Initializes the connection to the persistent subscription.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EventStoreStorageException"></exception>
    public async Task Init()
    {
        var timer = Stopwatch.StartNew();
        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("EventStoreQueueStorage for queue {QueueName} is initializing.", _queueName);
            }
            _client = new EventStoreClient(_queueOptions.ClientSettings);
            _subscriptionClient = new EventStorePersistentSubscriptionsClient(_queueOptions.ClientSettings);
            try
            {
                // Create a persistent subscription if it does not exist. 
                await _subscriptionClient.CreateAsync(_queueName, _groupName, _queueOptions.SubscriptionSettings, null, _queueOptions.Credentials);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Init: {Message}", ex.Message);
                }
            }
            _subscription = await _subscriptionClient.SubscribeToStreamAsync(_queueName, _groupName, OnEventAppeared, OnSubscriptionDropped, _queueOptions.Credentials, _queueOptions.QueueBufferSize);
            _initialized = true;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                timer.Stop();
                _logger.LogDebug("Init: QueueName={QueueName}, initialized in {ElapsedMilliseconds} ms", _queueName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            }
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Init: QueueName={QueueName}, errored in {ElapsedMilliseconds} ms.", _queueName, timer.Elapsed.TotalMilliseconds.ToString("0.00"));
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public async Task Close()
    {
        if (_initialized == false || _subscription == null || _subscriptionClient == null || _client == null)
        {
            return;
        }
        try
        {
            _eventQueue.Clear();
            _subscription.Dispose();
            await _subscriptionClient.DisposeAsync();
            await _client.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Close: QueueName={QueueName}", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _subscription = null;
            _subscriptionClient = null;
            _client = null;
            _initialized = false;
        }
    }

    #endregion

    #region Internal Queue Handlers

    private async Task OnEventAppeared(PersistentSubscription subscription, ResolvedEvent resolvedEvent, int? retryCount, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("EventAppeared from subscription: {Subscription} for the queue: {QueueName}", subscription.SubscriptionId, _queueName);
        }
        try
        {
            if (_eventQueue.All(evt => evt.Event.EventId != resolvedEvent.Event.EventId))
            {
                _eventQueue.Enqueue(resolvedEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to handle event from subscription for the queue {QueueName}.", _queueName);
            await subscription.Nack(PersistentSubscriptionNakEventAction.Retry, ex.Message, resolvedEvent);
        }
    }

    private async void OnSubscriptionDropped(PersistentSubscription subscription, SubscriptionDroppedReason reason, Exception? exception)
    {
        try
        {
            switch (reason)
            {
                case SubscriptionDroppedReason.ServerError:
                    _logger.LogWarning("SubscriptionDropped from subscription: {Subscription} for the queue: {QueueName} with server error: {Error}", subscription.SubscriptionId, _queueName, exception?.Message);
                    await ResubscribeAsync(10);
                    break;
                case SubscriptionDroppedReason.SubscriberError:
                    _logger.LogWarning("SubscriptionDropped from subscription: {Subscription} for the queue: {QueueName} with client error: {Error}", subscription.SubscriptionId, _queueName, exception?.Message);
                    await ResubscribeAsync(1);
                    break;
                case SubscriptionDroppedReason.Disposed:
                    _logger.LogInformation("SubscriptionDropped from subscription: {Subscription} for the queue: {QueueName} successfully", subscription.SubscriptionId, _queueName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to handle subscription dropped for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
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
                _subscription = await _subscriptionClient.SubscribeToStreamAsync(_queueName, _groupName, OnEventAppeared, OnSubscriptionDropped, _queueOptions.Credentials, _queueOptions.QueueBufferSize);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Successfully reconnected to the queue {QueueName}.", _queueName);
                }
                return;
            }
            catch (Exception)
            {
                _logger.LogError("Failed to reconnect to the queue {QueueName}. Retrying... Attempt: {Attempt}", _queueName, i + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
            }
        }
        _logger.LogError("Failed to reconnect to the queue {QueueName} after {Retries} attempts. Aborting...", _queueName, retryCount);
    }

    #endregion

    #region Storage

    /// <summary>
    ///     Peeks in the queue for latest event, without dequeuing it.
    /// </summary>
    public ResolvedEvent? Peek()
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Peeking event from internal queue: {QueueName}", _queueName);
        }
        if (_initialized == false)
        {
            return null;
        }
        try
        {
            _eventQueue.TryPeek(out var resolvedEvent);
            return resolvedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to peek event from internal queue for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to peek event from internal queue for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Peeks in the queue for latest event, without dequeuing it.
    /// </summary>
    public IReadOnlyList<ResolvedEvent> PeekMany(int maxCount)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Peeking events from internal queue: {QueueName}", _queueName);
        }
        var resolvedEvents = _eventsPool.GetList();
        if (_initialized == false)
        {
            return resolvedEvents;
        }
        try
        {
            for (var i = 0; i < maxCount; i++)
            {
                if (!_eventQueue.TryPeek(out var resolvedEvent))
                {
                    break;
                }
                resolvedEvents.Add(resolvedEvent);
            }
            return resolvedEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to peek events from internal queue for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to peek events from internal queue for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _eventsPool.ReturnList(resolvedEvents);
        }
    }

    /// <summary>
    ///     Dequeues in the queue for latest event.
    /// </summary>
    public ResolvedEvent? Dequeue()
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Dequeuing event from internal queue: {QueueName}", _queueName);
        }
        if (_initialized == false)
        {
            return null;
        }
        try
        {
            _eventQueue.TryDequeue(out var resolvedEvent);
            return resolvedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to dequeue event from internal queue for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to dequeue event from internal queue for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Dequeues in the queue for latest event.
    /// </summary>
    public IReadOnlyList<ResolvedEvent> DequeueMany(int maxCount)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Dequeuing events from internal queue: {QueueName}", _queueName);
        }
        var resolvedEvents = _eventsPool.GetList();
        if (_initialized == false)
        {
            return resolvedEvents;
        }
        try
        {
            for (var i = 0; i < maxCount; i++)
            {
                if (!_eventQueue.TryDequeue(out var resolvedEvent))
                {
                    break;
                }
                resolvedEvents.Add(resolvedEvent);
            }
            return resolvedEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to dequeue events from internal queue for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to dequeue events from internal queue for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
        finally
        {
            _eventsPool.ReturnList(resolvedEvents);
        }
    }

    /// <summary>
    ///     Acknowledge the event, it will be deleted from server message queue.
    /// </summary>
    public async Task AcknowledgeAsync(params ResolvedEvent[] resolvedEvents)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Acknowledging event to queue: {QueueName}", _queueName);
        }
        if (_initialized == false || _subscription == null)
        {
            return;
        }
        try
        {
            await _subscription.Ack(resolvedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to acknowledge event for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to acknowledge event for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Unacknowledge the event, it will be moved from server message queue to poison queue.
    /// </summary>
    public async Task UnacknowledgeAsync(PersistentSubscriptionNakEventAction action, string reason, params ResolvedEvent[] resolvedEvents)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Unacknowledging event to queue: {QueueName}", _queueName);
        }
        if (_initialized == false || _subscription == null)
        {
            return;
        }
        try
        {
            await _subscription.Nack(action, reason, resolvedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to unacknowledge event for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to unacknowledge event for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Adds a new message to the queue.
    /// </summary>
    /// <param name="message">Message to be added to the queue.</param>
    public async Task AppendAsync(EventData message)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Appending message to queue: {QueueName}", _queueName);
        }
        if (_initialized == false || _client == null || message == null)
        {
            return;
        }
        try
        {
            await _client.AppendToStreamAsync(_queueName, StreamState.Any, new[] { message }, null, null, _queueOptions.Credentials).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write message for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write message for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Adds new messages to the queue.
    /// </summary>
    /// <param name="messages">Messages to be added to the queue.</param>
    public async Task AppendManyAsync(IList<EventData> messages)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Appending messages to queue: {QueueName}", _queueName);
        }
        if (_initialized == false || _client == null || messages == null || messages.Count == 0)
        {
            return;
        }
        try
        {
            await _client.AppendToStreamAsync(_queueName, StreamState.Any, messages, null, null, _queueOptions.Credentials).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write messages for the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write messages for the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InconsistentStateException"></exception>
    /// <exception cref="EventStoreStorageException"></exception>
    public async Task DeleteQueueAsync()
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Deleting queue: {QueueName}", _queueName);
        }
        if (_initialized == false || _subscription == null || _subscriptionClient == null || _client == null)
        {
            return;
        }
        try
        {
            _subscription.Dispose();
            await _subscriptionClient.DeleteAsync(_queueName, _groupName, null, _queueOptions.Credentials);
            await _client.DeleteAsync(_queueName, StreamState.Any).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete the queue {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to delete the queue {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    #endregion

    #region Helper

    private string SanitizeQueueName(string queueName)
    {
        var sanitizedName = queueName;
        sanitizedName = sanitizedName.ToLowerInvariant();
        sanitizedName = sanitizedName.Replace('/', '-') // Forward slash
                                     .Replace('\\', '-') // Backslash
                                     .Replace('#', '-') // Pound sign
                                     .Replace('?', '-') // Question mark
                                     .Replace('&', '-')
                                     .Replace('+', '-')
                                     .Replace(':', '-')
                                     .Replace('.', '-')
                                     .Replace('%', '-');
        return sanitizedName;
    }

    private void ValidateQueueName(string queueName)
    {
        if (!(queueName.Length is >= 3 and <= 63))
        {
            // A queue name must be from 3 through 63 characters long.
            throw new ArgumentException(string.Format("A queue name must be from 3 through 63 characters long, while your queueName length is {0}, queueName is {1}.", queueName.Length, queueName), queueName);
        }
        if (!char.IsLetterOrDigit(queueName.First()))
        {
            // A queue name must start with a letter or number
            throw new ArgumentException(string.Format("A queue name must start with a letter or number, while your queueName is {0}.", queueName), queueName);
        }
        if (!char.IsLetterOrDigit(queueName.Last()))
        {
            // The first and last letters in the queue name must be alphanumeric. The dash (-) character cannot be the first or last character.
            throw new ArgumentException(string.Format("The last letter in the queue name must be alphanumeric, while your queueName is {0}.", queueName), queueName);
        }
        if (!queueName.All(c => char.IsLetterOrDigit(c) || c.Equals('-')))
        {
            // A queue name can only contain letters, numbers, and the dash (-) character.
            throw new ArgumentException(string.Format("A queue name can only contain letters, numbers, and the dash (-) character, while your queueName is {0}.", queueName), queueName);
        }
        if (queueName.Contains("--"))
        {
            // Consecutive dash characters are not permitted in the queue name.
            throw new ArgumentException(string.Format("Consecutive dash characters are not permitted in the queue name, while your queueName is {0}.", queueName), queueName);
        }
        if (queueName.Where(char.IsLetter).Any(c => !char.IsLower(c)))
        {
            // All letters in a queue name must be lowercase.
            throw new ArgumentException(string.Format("All letters in a queue name must be lowercase, while your queueName is {0}.", queueName), queueName);
        }
    }

    #endregion

}
