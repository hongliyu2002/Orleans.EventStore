namespace SiloX.Domain.Abstractions;

/// <summary>
///     Interface for traceable objects that contains the trace ID, operation timestamp and operator information.
/// </summary>
public interface ITraceable
{
    /// <summary>
    ///     The trace ID associated with this traceable object.
    /// </summary>
    public Guid TraceId { get; init; }

    /// <summary>
    ///     The timestamp when the operation is performed.
    /// </summary>
    public DateTimeOffset OperatedAt { get; init; }

    /// <summary>
    ///     The operator information who performs the operation.
    /// </summary>
    public string OperatedBy { get; init; }
}
