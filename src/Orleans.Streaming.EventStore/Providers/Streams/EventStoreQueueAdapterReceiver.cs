using EventStore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Receives batches of messages from a single partition of a message queue.
/// </summary>
internal class EventStoreQueueAdapterReceiver : IQueueAdapterReceiver
{
    private record PendingDelivery(StreamSequenceToken SequenceToken, ResolvedEvent Event);

    private readonly string _queueName;
    private EventStoreQueueStorage? _queueStorage;
    private readonly IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> _dataAdapter;
    private readonly ILogger<EventStoreQueueAdapterReceiver> _logger;
    private readonly List<PendingDelivery> _pendingDeliveries = new(32);

    private Task? _task;

    public static IQueueAdapterReceiver Create(string queueName, EventStoreQueueOptions queueOptions, IOptions<ClusterOptions> clusterOptions, IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> dataAdapter, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(queueName, nameof(queueName));
        ArgumentNullException.ThrowIfNull(queueOptions, nameof(queueOptions));
        ArgumentNullException.ThrowIfNull(clusterOptions, nameof(clusterOptions));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        var streamStorage = new EventStoreQueueStorage(queueName, clusterOptions.Value.ServiceId, queueOptions, loggerFactory.CreateLogger<EventStoreQueueStorage>());
        return new EventStoreQueueAdapterReceiver(queueName, streamStorage, dataAdapter, loggerFactory.CreateLogger<EventStoreQueueAdapterReceiver>());
    }

    private EventStoreQueueAdapterReceiver(string queueName, EventStoreQueueStorage queueStorage, IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> dataAdapter, ILogger<EventStoreQueueAdapterReceiver> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(queueName, nameof(queueName));
        ArgumentNullException.ThrowIfNull(queueStorage, nameof(queueStorage));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _queueName = queueName;
        _queueStorage = queueStorage;
        _dataAdapter = dataAdapter;
        _logger = logger;
    }

    #region Lifecycle

    /// <summary>
    ///     Initializes this receiver.
    /// </summary>
    /// <returns>A <see cref="Task" />representing the operation.</returns>
    public Task Initialize(TimeSpan timeout)
    {
        if (_queueStorage != null)
        {
            return _queueStorage.Init();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Receiver is no longer used. Shutdown and clean up.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the operation.</returns>
    public async Task Shutdown(TimeSpan timeout)
    {
        try
        {
            // await the last storage operation, so after we shutdown and stop this receiver we don't get async operation completions from pending storage operations.
            if (_task != null)
            {
                await _task;
            }
            if (_queueStorage != null)
            {
                await _queueStorage.Close();
            }
        }
        finally
        {
            _queueStorage = null;
        }
    }

    #endregion

    #region IQueueAdapterReceiver Implementation

    /// <summary>
    ///     Retrieves batches from a message queue.
    /// </summary>
    /// <param name="maxCount">
    ///     The maximum number of message batches to retrieve.
    /// </param>
    /// <returns>The message batches.</returns>
    public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        const int MaxNumberOfMessagesToPeek = 32;
        try
        {
            // store direct ref, in case we are somehow asked to shutdown while we are receiving.
            var queueStorage = _queueStorage;
            if (queueStorage == null)
            {
                return Task.FromResult<IList<IBatchContainer>>(new List<IBatchContainer>());
            }
            var count = maxCount < 0 ? MaxNumberOfMessagesToPeek : Math.Min(maxCount, MaxNumberOfMessagesToPeek);
            var resolvedEvents = queueStorage.DequeueMany(count);
            var batchContainers = new List<IBatchContainer>(resolvedEvents.Count);
            foreach (var resolvedEvent in resolvedEvents)
            {
                var batchContainer = _dataAdapter.FromQueueMessage(resolvedEvent.Event.Data, (long)resolvedEvent.Event.Position.CommitPosition * 1000);
                batchContainers.Add(batchContainer);
                _pendingDeliveries.Add(new PendingDelivery(batchContainer.SequenceToken, resolvedEvent));
            }
            return Task.FromResult<IList<IBatchContainer>>(batchContainers);
        }
        finally
        {
            _task = null;
        }
    }

    /// <summary>
    ///     Notifies the adapter receiver that the messages were delivered to all consumers,
    ///     so the receiver can take an appropriate action (e.g., delete the messages from a message queue).
    /// </summary>
    /// <param name="deliveredContainers">
    ///     The message batches.
    /// </param>
    /// <returns>A <see cref="Task" /> representing the operation.</returns>
    public async Task MessagesDeliveredAsync(IList<IBatchContainer> deliveredContainers)
    {
        try
        {
            // store direct ref, in case we are somehow asked to shutdown while we are receiving.
            var queueStorage = _queueStorage;
            if (queueStorage == null || deliveredContainers == null || deliveredContainers.Count == 0)
            {
                return;
            }
            // get sequence tokens of delivered messages
            var deliveredTokens = deliveredContainers.Select(container => container.SequenceToken).ToList();
            // find latest delivered message
            var latestToken = deliveredTokens.Max();
            // finalize all pending messages at or before the latest
            var finalizedDeliveries = _pendingDeliveries.Where(pendingDelivery => !pendingDelivery.SequenceToken.Newer(latestToken)).ToList();
            if (finalizedDeliveries.Count == 0)
            {
                return;
            }
            // remove all finalized deliveries from pending, regardless of if it was delivered or not.
            _pendingDeliveries.RemoveRange(0, finalizedDeliveries.Count);
            // get the queue messages for all finalized deliveries that were delivered.
            var deliveredEvents = finalizedDeliveries.Where(finalized => deliveredTokens.Contains(finalized.SequenceToken)).Select(finalized => finalized.Event).ToList();
            if (deliveredEvents.Count == 0)
            {
                return;
            }
            // Acknowlege all delivered queue messages from the queue.  Anything finalized but not delivered will show back up later
            _task = queueStorage.AcknowledgeAsync(deliveredEvents.ToArray());
            try
            {
                await _task;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception upon Acknowledge on queue {QueueName}. Ignoring.", _queueName);
            }
        }
        finally
        {
            _task = null;
        }
    }

    #endregion

}
