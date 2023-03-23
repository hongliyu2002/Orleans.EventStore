namespace Orleans.Streaming.EventStoreStorage;

/// <summary>
///     An interface defining the required properties for a stream state model.
///     Custom state model types must implement this interface.
/// </summary>
/// <remarks>
///     Two options exist for implementations of <see cref="IEventStoreState" />:
///     Strongly typed custom state model classes.
/// </remarks>
public interface IEventStoreState
{
    /// <summary>
    ///     The Id value for the state.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    ///     The ETag value for the state.
    /// </summary>
    string ETag { get; set; }
}
