using EventStore.Client;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Streaming.EventStoreStorage;
using Orleans.Streams;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     This class stores EventStore partition checkpointer information (a position) in another EventStore stream.
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
    ///     Factory function that creates and initializes the checkpointer.
    /// </summary>
    /// <param name="serviceId"></param>
    /// <param name="streamProviderName"></param>
    /// <param name="partition"></param>
    /// <param name="options"></param>
    /// <param name="loggerFactory"></param>
    /// <returns></returns>
    public static EventStoreCheckpointer Create(string serviceId, string streamProviderName, string partition, EventStoreStreamCheckpointerOptions options, ILoggerFactory loggerFactory)
    {
        var checkpointer = new EventStoreCheckpointer(serviceId, streamProviderName, partition, options, loggerFactory);
        checkpointer.Initialize();
        return checkpointer;
    }

    private EventStoreCheckpointer(string serviceId, string streamProviderName, string partition, EventStoreStreamCheckpointerOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceId, nameof(serviceId));
        ArgumentException.ThrowIfNullOrEmpty(streamProviderName, nameof(streamProviderName));
        ArgumentException.ThrowIfNullOrEmpty(partition, nameof(partition));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        // _logger = loggerFactory.CreateLogger<EventStoreCheckpointer>();
        // _logger.LogInformation("Creating EventStore checkpointer for partition {Partition} of stream provider {StreamProviderName} with serviceId {ServiceId}.", partition, streamProviderName, serviceId);
        _persistInterval = options.PersistInterval;
        _stateManager = new EventStoreStateManager(options, loggerFactory.CreateLogger<EventStoreStateManager>());
        _checkPointState = new EventStoreCheckpointState();
        _streamName = EventStoreCheckpointState.GetStreamName(serviceId, streamProviderName, partition);
    }

    private void Initialize()
    {
        _stateManager.Init();
    }

    /// <summary>
    ///     Gets a value indicating whether a checkpoint exists.
    /// </summary>
    /// <value><see langword="true" /> if checkpoint exists; otherwise, <see langword="false" />.</value>
    public bool CheckpointExists
    {
        get
        {
            if (_checkPointState is not { Position: { } })
            {
                return false;
            }
            return _checkPointState.Position != Position.Start.ToString();
        }
    }

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
