using System.Collections.Concurrent;
using System.Diagnostics;
using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Orleans.Storage;
using Orleans.Streaming.Configuration;

namespace Orleans.Streaming.EventStoreStorage;

/// <summary>
///     EventStore-based queue storage provider based on persistent subscription feature.
/// </summary>
public class EventStoreQueueStorage
{
    private readonly string _queueName;
    private readonly string _groupName;
    private readonly EventStoreStorageOptions _storageOptions;
    private readonly ILogger<EventStoreQueueStorage> _logger;

    private EventStoreClient _client = null!;
    private EventStorePersistentSubscriptionsClient _subscriptionClient = null!;
    private PersistentSubscription _subscription = null!;

    private readonly ConcurrentQueue<ResolvedEvent> _eventQueue = new();

    /// <summary>
    ///     Creates a new instance of the <see cref="EventStoreQueueStorage" /> type.
    /// </summary>
    /// <param name="queueName">Name of the queue to be connected to.</param>
    /// <param name="groupName"></param>
    /// <param name="storageOptions"></param>
    /// <param name="logger"></param>
    public EventStoreQueueStorage(string queueName, string groupName, EventStoreStorageOptions storageOptions, ILogger<EventStoreQueueStorage> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(queueName, nameof(queueName));
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(groupName));
        ArgumentNullException.ThrowIfNull(storageOptions, nameof(storageOptions));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        queueName = SanitizeQueueName(queueName);
        ValidateQueueName(queueName);
        _queueName = queueName;
        _groupName = groupName;
        _storageOptions = storageOptions;
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
            _client = new EventStoreClient(_storageOptions.ClientSettings);
            _subscriptionClient = new EventStorePersistentSubscriptionsClient(_storageOptions.ClientSettings);
            try
            {
                // Create a persistent subscription if it does not exist. 
                await _subscriptionClient.CreateAsync(_queueName, _groupName, _storageOptions.SubscriptionSettings, null, _storageOptions.Credentials);
            }
            catch (RpcException ex) when (ex.StatusCode is StatusCode.AlreadyExists)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Init: {Message}", ex.Message);
                }
            }
            _subscription = await _subscriptionClient.SubscribeToStreamAsync(_queueName, _groupName, OnEventAppeared, OnSubscriptionDropped, _storageOptions.Credentials, _storageOptions.SubscriptionQueueBufferSize);
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
        try
        {
            _subscription.Dispose();
            await _subscriptionClient.DisposeAsync();
            await _client.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Close: QueueName={QueueName}", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"{ex.GetType()}: {ex.Message}"));
        }
    }

    #endregion

    #region Internal Queue Operations

    private Task OnEventAppeared(PersistentSubscription arg1, ResolvedEvent arg2, int? arg3, CancellationToken arg4)
    {
        return null;
    }

    private void OnSubscriptionDropped(PersistentSubscription arg1, SubscriptionDroppedReason arg2, Exception? arg3)
    {
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
        try
        {
            _eventQueue.TryPeek(out var resolvedEvent);
            return resolvedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to peek event from internal queue for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to peek event from internal queue for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
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
        try
        {
            var resolvedEvents = new List<ResolvedEvent>(4);
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
            _logger.LogError("Failed to peek events from internal queue for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to peek events from internal queue for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
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
        try
        {
            _eventQueue.TryDequeue(out var resolvedEvent);
            return resolvedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to dequeue event from internal queue for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to dequeue event from internal queue for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
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
        try
        {
            var resolvedEvents = new List<ResolvedEvent>(4);
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
            _logger.LogError("Failed to dequeue events from internal queue for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to dequeue events from internal queue for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Acknowledge the event, it will be deleted from server message queue.
    /// </summary>
    public async Task AcknowledgeAsync(params ResolvedEvent[] resolvedEvents)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Acknowledge event to stream: {QueueName}", _queueName);
        }
        try
        {
            await _subscription.Ack(resolvedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to acknowledge event for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to acknowledge event for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Unacknowledge the event, it will be moved from server message queue to poison queue.
    /// </summary>
    public async Task UnacknowledgeAsync(PersistentSubscriptionNakEventAction action, string reason, params ResolvedEvent[] resolvedEvents)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Unacknowledge event to stream: {QueueName}", _queueName);
        }
        try
        {
            await _subscription.Nack(action, reason, resolvedEvents);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to unacknowledge event for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to unacknowledge event for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Adds a new message to the queue.
    /// </summary>
    /// <param name="message">Message to be added to the queue.</param>
    public async Task<long> AppendAsync(EventData message)
    {
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Appending message to stream: {QueueName}", _queueName);
        }
        try
        {
            var writeResult = await _client.AppendToStreamAsync(_queueName, StreamState.Any, new[] { message }, null, null, _storageOptions.Credentials).ConfigureAwait(false);
            return (long)writeResult.LogPosition.CommitPosition;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write message for the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to write message for the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InconsistentStateException"></exception>
    /// <exception cref="EventStoreStorageException"></exception>
    public async Task DeleteQueueAsync()
    {
        try
        {
            _subscription.Dispose();
            await _subscriptionClient.DeleteAsync(_queueName, _groupName, null, _storageOptions.Credentials);
            await _client.DeleteAsync(_queueName, StreamState.Any).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete the stream {QueueName}.", _queueName);
            throw new EventStoreStorageException(FormattableString.Invariant($"Failed to delete the stream {_queueName}. {ex.GetType()}: {ex.Message}"));
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
