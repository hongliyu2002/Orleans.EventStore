using Microsoft.Extensions.Logging;

namespace Orleans.Streaming.EventStoreStorage;

/// <summary>
/// </summary>
internal static class EventStoreErrorCodes
{
    internal static readonly EventId CannotInitializeClient = new(100101, nameof(CannotInitializeClient));
    internal static readonly EventId CannotReadStateFromStream = new(100104, nameof(CannotReadStateFromStream));
    internal static readonly EventId CannotWriteStateToStream = new(100105, nameof(CannotWriteStateToStream));
    internal static readonly EventId CannotClearStateToStream = new(100105, nameof(CannotClearStateToStream));
    internal static readonly EventId VersionConflictInStream = new(100106, nameof(VersionConflictInStream));

    internal static readonly EventId SlowAccessToStream = new(100115, nameof(SlowAccessToStream));
    internal static readonly EventId FailCreatingClient = new(100118, nameof(FailCreatingClient));
    internal static readonly EventId FailDisposingClient = new(100118, nameof(FailDisposingClient));
    
    internal static readonly EventId CannotInitializeSubscriptionClient = new(100201, nameof(CannotInitializeSubscriptionClient));
    internal static readonly EventId CannotReadFromSubscription = new(100204, nameof(CannotReadFromSubscription));
    internal static readonly EventId CannotWriteToSubscription = new(100205, nameof(CannotWriteToSubscription));
}
