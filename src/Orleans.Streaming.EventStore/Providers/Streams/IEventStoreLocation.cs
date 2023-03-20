using EventStore.Client;

namespace Orleans.Streaming.Providers.Streams;

/// <summary>
///     Location of a message within an EventStore.
/// </summary>
public interface IEventStoreLocation
{
    /// <summary>
    ///     A potential logical record position in the Event Store transaction file.
    /// </summary>
    string Offset { get; }

    /// <summary>
    ///     A position within a stream.
    /// </summary>
    long SequenceNumber { get; }
}
