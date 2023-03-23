namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Location of a message within an EventStore.
/// </summary>
public interface IEventStoreLocation
{
    /// <summary>
    ///     Stream position of the event within an EventStore.
    /// </summary>
    string Position { get; }

    /// <summary>
    ///     EventStore sequence id of the message
    /// </summary>
    long SequenceNumber { get; }
}
