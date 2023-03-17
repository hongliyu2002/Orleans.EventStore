using Orleans.Serialization;
using Orleans.Storage;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     A log-consistency provider that stores the latest view in primary storage, using any standard storage provider.
///     Supports multiple clusters connecting to the same primary storage (doing optimistic concurrency control via e-tags)
///     <para>
///         The log itself is actually saved to storage - the latest view (snapshot) and metadata (the log position, and write flags)
///         and all log entries are stored in the primary.
///     </para>
/// </summary>
public class LogConsistencyProvider : ILogViewAdaptorFactory
{
    private readonly ILogConsistentStorage _logConsistentStorage;
    private readonly DeepCopier _deepCopier;

    /// <summary>
    ///     Initializes a new instance of LogConsistencyProvider class
    /// </summary>
    /// <param name="logConsistentStorage"></param>
    /// <param name="deepCopier"></param>
    public LogConsistencyProvider(ILogConsistentStorage logConsistentStorage, DeepCopier deepCopier)
    {
        ArgumentNullException.ThrowIfNull(logConsistentStorage);
        ArgumentNullException.ThrowIfNull(deepCopier);
        _logConsistentStorage = logConsistentStorage;
        _deepCopier = deepCopier;
    }

    /// <inheritdoc />
    public ILogViewAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, TLogView initialState, string grainTypeName, IGrainStorage grainStorage, ILogConsistencyProtocolServices services)
        where TLogView : class, new()
        where TLogEntry : class
    {
        return new LogViewAdaptor<TLogView, TLogEntry>(hostGrain, initialState, grainStorage, grainTypeName, services, _logConsistentStorage, _deepCopier);
    }

    /// <inheritdoc />
    public bool UsesStorageProvider => true;
}
