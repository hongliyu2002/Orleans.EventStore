using System.Runtime.Serialization;

namespace Orleans.Providers.Streams.EventStore;

/// <summary>
///     Exception for throwing from EventStore stream storage.
/// </summary>
[GenerateSerializer]
public class EventStoreStorageException : Exception
{
    /// <summary>
    ///     Initializes a new instance of <see cref="EventStoreStorageException" />.
    /// </summary>
    public EventStoreStorageException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="EventStoreStorageException" />.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public EventStoreStorageException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="EventStoreStorageException" />.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public EventStoreStorageException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <inheritdoc />
    protected EventStoreStorageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
