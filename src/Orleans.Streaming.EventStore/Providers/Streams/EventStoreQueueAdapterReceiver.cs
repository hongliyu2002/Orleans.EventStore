using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Streaming.Configuration;
using Orleans.Streaming.EventStoreStorage;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Receives batches of messages from a single partition of a message queue.
/// </summary>
internal class EventStoreQueueAdapterReceiver : IQueueAdapterReceiver
{
    public static IQueueAdapterReceiver Create(string streamName, EventStoreStorageOptions storageOptions, IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> dataAdapter, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName, nameof(streamName));
        ArgumentNullException.ThrowIfNull(storageOptions, nameof(storageOptions));
        ArgumentNullException.ThrowIfNull(dataAdapter, nameof(dataAdapter));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        var streamStorage = new EventStoreStreamStorage(streamName, storageOptions, loggerFactory.CreateLogger<EventStoreStreamStorage>());
        return new EventStoreQueueAdapterReceiver(streamName, streamStorage, dataAdapter, loggerFactory.CreateLogger<EventStoreQueueAdapterReceiver>());
    }

    private readonly string _streamName;
    private EventStoreStreamStorage? _streamStorage;
    private readonly IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> _dataAdapter;
    private readonly ILogger<EventStoreQueueAdapterReceiver> _logger;
    private readonly List<PendingDelivery> _pendings;
    private Task? _outstandingTask;
    private long lastReadMessage;

    private EventStoreQueueAdapterReceiver(string streamName, EventStoreStreamStorage streamStorage, IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer> dataAdapter, ILogger<EventStoreQueueAdapterReceiver> logger)
    {
        _streamName = streamName;
        _streamStorage = streamStorage;
        _dataAdapter = dataAdapter;
        _logger = logger;
        _pendings = new List<PendingDelivery>(32);
    }

    /// <summary>
    ///     Initializes this receiver.
    /// </summary>
    /// <returns>A <see cref="Task" />representing the operation.</returns>
    public Task Initialize(TimeSpan timeout)
    {
        return _streamStorage == null ? Task.CompletedTask : _streamStorage.Init();
    }

    /// <summary>
    ///     Receiver is no longer used. Shutdown and clean up.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the operation.</returns>
    public async Task Shutdown(TimeSpan timeout)
    {
        try
        {
            if (_outstandingTask != null)
            {
                await _outstandingTask;
            }
            if (_streamStorage != null)
            {
                await _streamStorage.Close();
            }
        }
        finally
        {
            _streamStorage = null;
        }
    }

    /// <summary>
    ///     Retrieves batches from a message queue.
    /// </summary>
    /// <param name="maxCount">
    ///     The maximum number of message batches to retrieve.
    /// </param>
    /// <returns>The message batches.</returns>
    public async Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        const int MaxNumberOfMessagesToPeek = 32;
        try
        {
            // store direct ref, in case we are somehow asked to shutdown while we are receiving.
            var streamStorage = _streamStorage;
            if (streamStorage == null)
            {
                return new List<IBatchContainer>();
            }
            var count = maxCount < 0 ? MaxNumberOfMessagesToPeek : Math.Min(maxCount, MaxNumberOfMessagesToPeek);
            var readLastTask = streamStorage.ReadLastAsync(count);
            _outstandingTask = readLastTask;
            var messages = await readLastTask;
            var containers = new List<IBatchContainer>();
            foreach (var message in messages)
            {
                var container = _dataAdapter.FromQueueMessage(message.Data, lastReadMessage++);
                containers.Add(container);
                _pendings.Add(new PendingDelivery(container.SequenceToken, message));
            }
            return containers;
        }
        finally
        {
            _outstandingTask = null;
        }
    }

    /// <summary>
    ///     Notifies the adapter receiver that the messages were delivered to all consumers,
    ///     so the receiver can take an appropriate action (e.g., delete the messages from a message queue).
    /// </summary>
    /// <param name="messages">
    ///     The message batches.
    /// </param>
    /// <returns>A <see cref="Task" /> representing the operation.</returns>
    public async Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
    {
        try
        {
            var streamStorage = _streamStorage;
            if (streamStorage == null || messages.Count == 0)
            {
                return;
            }
        }
        finally
        {
            _outstandingTask = null;
        }
    }

    private class PendingDelivery
    {
        public PendingDelivery(StreamSequenceToken token, EventRecord message)
        {
            Token = token;
            Message = message;
        }

        public EventRecord Message { get; }

        public StreamSequenceToken Token { get; }
    }
}
