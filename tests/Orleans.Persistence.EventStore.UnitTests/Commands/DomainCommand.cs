namespace SiloX.Domain.Abstractions;

/// <summary>
///     Represents a domain command.
/// </summary>
/// <param name="TraceId">The unique identifier for the trace.</param>
/// <param name="OperatedAt">The date and time when the operation was performed.</param>
/// <param name="OperatedBy">The name of the operator who performed the operation.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public abstract record DomainCommand
    (Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : ITraceable;
