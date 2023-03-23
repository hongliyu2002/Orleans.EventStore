using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Streaming.EventStoreStorage;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     This class stores EventStore queue checkpointer information (a position) in another EventStore stream.
/// </summary>
public class EventStoreCheckpointer : IStreamQueueCheckpointer<string>
{
    // private readonly ILogger<EventStoreCheckpointer> _logger;
    private readonly TimeSpan _persistInterval;
    private readonly EventStoreStateManager _stateManager;
    private EventStoreCheckpointState _checkPointState;
    private readonly string _streamName;

    private Task? _inProgressSaveTask;
    private DateTime? _throttleSavesUntilUtc;

    /// <summary>
    ///     A factory method that creates a new instance of <see cref="EventStoreCheckpointer" /> with the specified parameters.
    /// </summary>
    /// <param name="serviceId">The ID of the service that the checkpointer will be created for.</param>
    /// <param name="streamProviderName">The name of the stream provider that the checkpointer will be created for.</param>
    /// <param name="queue">The name of the queue that the checkpointer will be created for.</param>
    /// <param name="options">The options used to configure the checkpointer.</param>
    /// <param name="loggerFactory">The logger factory used to create loggers.</param>
    /// <returns>A new instance of <see cref="EventStoreCheckpointer" />.</returns>
    public static EventStoreCheckpointer Create(string serviceId, string streamProviderName, string queue, EventStoreStreamCheckpointerOptions options, ILoggerFactory loggerFactory)
    {
        var checkpointer = new EventStoreCheckpointer(serviceId, streamProviderName, queue, options, loggerFactory);
        checkpointer.Initialize();
        return checkpointer;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreCheckpointer" /> class.
    /// </summary>
    /// <param name="serviceId">The ID of the service that the checkpointer will be created for.</param>
    /// <param name="streamProviderName">The name of the stream provider that the checkpointer will be created for.</param>
    /// <param name="queue">The name of the queue that the checkpointer will be created for.</param>
    /// <param name="options">The options used to configure the checkpointer.</param>
    /// <param name="loggerFactory">The logger factory used to create loggers.</param>
    private EventStoreCheckpointer(string serviceId, string streamProviderName, string queue, EventStoreStreamCheckpointerOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceId, nameof(serviceId));
        ArgumentException.ThrowIfNullOrEmpty(streamProviderName, nameof(streamProviderName));
        ArgumentException.ThrowIfNullOrEmpty(queue, nameof(queue));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        // _logger = loggerFactory.CreateLogger<EventStoreCheckpointer>();
        // _logger.LogInformation("Creating EventStore checkpointer for queue {Partition} of stream provider {StreamProviderName} with serviceId {ServiceId}.", queue, streamProviderName, serviceId);
        _persistInterval = options.PersistInterval;
        _stateManager = new EventStoreStateManager(options, loggerFactory.CreateLogger<EventStoreStateManager>());
        _checkPointState = new EventStoreCheckpointState();
        _streamName = EventStoreCheckpointState.GetStreamName(serviceId, streamProviderName, queue);
    }

    private void Initialize()
    {
        _stateManager.Init();
    }

    /// <summary>
    ///     Gets a value indicating whether a checkpoint exists.
    /// </summary>
    /// <value><see langword="true" /> if checkpoint exists; otherwise, <see langword="false" />.</value>
    public bool CheckpointExists => _checkPointState is { Position: { } } && !string.IsNullOrEmpty(_checkPointState.Position);

    /// <summary>
    ///     Loads the checkpoint.
    /// </summary>
    /// <returns>The checkpoint.</returns>
    public async Task<string> Load()
    {
        var checkpointState = await _stateManager.ReadStateAsync<EventStoreCheckpointState>(_streamName);
        if (checkpointState != null)
        {
            _checkPointState = checkpointState;
        }
        return _checkPointState.Position;
    }

    /// <summary>
    ///     Updates the checkpoint.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="utcNow">The current UTC time.</param>
    public void Update(string position, DateTime utcNow)
    {
        // if position has not changed, do nothing
        if (string.Compare(_checkPointState.Position, position, StringComparison.Ordinal) == 0)
        {
            return;
        }
        // if we've saved before but it's not time for another save or the last save operation has not completed, do nothing
        if (_inProgressSaveTask != null && _throttleSavesUntilUtc.HasValue && (_throttleSavesUntilUtc.Value > utcNow || !_inProgressSaveTask.IsCompleted))
        {
            return;
        }
        _checkPointState.Position = position;
        _throttleSavesUntilUtc = utcNow + _persistInterval;
        _inProgressSaveTask = _stateManager.WriteStateAsync(_streamName, _checkPointState);
        _inProgressSaveTask.Ignore();
    }
}
