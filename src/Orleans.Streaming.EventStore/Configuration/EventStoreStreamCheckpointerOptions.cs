namespace Orleans.Configuration;

/// <summary>
/// </summary>
public class EventStoreStreamCheckpointerOptions : EventStoreOperationOptions
{
    /// <summary>
    /// </summary>
    public static readonly TimeSpan DefaultCheckpointPersistInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Interval to write checkpoints.  Prevents spamming storage.
    /// </summary>
    public TimeSpan PersistInterval { get; set; } = DefaultCheckpointPersistInterval;
}
