using Orleans.Storage;

namespace Orleans.EventSourcing.EventStore;

public class LogConsistencyProvider : ILogViewAdaptorFactory
{

    /// <inheritdoc />
    public ILogViewAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(ILogViewAdaptorHost<TLogView, TLogEntry> hostgrain, TLogView initialstate, string graintypename, IGrainStorage grainStorage, ILogConsistencyProtocolServices services)
        where TLogView : class, new()
        where TLogEntry : class
    {
        return null;
    }

    /// <inheritdoc />
    public bool UsesStorageProvider { get; set; }
}
