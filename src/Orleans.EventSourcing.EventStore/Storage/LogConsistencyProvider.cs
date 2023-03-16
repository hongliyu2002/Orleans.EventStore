using Orleans.Storage;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     A log-consistency provider that stores the latest view in primary storage, using any standard storage provider.
///     Supports multiple clusters connecting to the same primary storage (doing optimistic concurrency control via e-tags)
///     <para>
///         The log itself is transient, i.e. not actually saved to storage - only the latest view (snapshot) and some
///         metadata (the log position, and write flags) are stored in the primary.
///     </para>
/// </summary>
public class LogConsistencyProvider : ILogViewAdaptorFactory
{
    private readonly ILogConsistentStorage _logConsistentStorage;

    /// <summary>
    ///     Initializes a new instance of LogConsistencyProvider class
    /// </summary>
    /// <param name="logConsistentStorage"></param>
    public LogConsistencyProvider(ILogConsistentStorage logConsistentStorage)
    {
        ArgumentNullException.ThrowIfNull(logConsistentStorage);
        _logConsistentStorage = logConsistentStorage;
    }

    /// <inheritdoc />
    public ILogViewAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, TLogView initialState, string grainTypeName, IGrainStorage grainStorage, ILogConsistencyProtocolServices services)
        where TLogView : class, new()
        where TLogEntry : class
    {
        return new LogViewAdaptor<TLogView, TLogEntry>(hostGrain, initialState, grainStorage, grainTypeName, services, _logConsistentStorage);
    }

    /// <inheritdoc />
    public bool UsesStorageProvider => true;
}
