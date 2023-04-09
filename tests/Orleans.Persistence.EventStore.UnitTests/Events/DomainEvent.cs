namespace SiloX.Domain.Abstractions;

/// <summary>
///     An abstract record represents a domain event.
/// </summary>
/// <param name="DomainId">The unique identifier of the original object who raises this event.</param>
/// <param name="Version">The version of the domain event.</param>
/// <param name="TraceId">The trace ID associated with this traceable object.</param>
/// <param name="OperatedAt">The timestamp when the operation is performed.</param>
/// <param name="OperatedBy">The operator information who performs the operation.</param>
[Immutable]
[Serializable]
[GenerateSerializer]
public abstract record DomainEvent
    (int Version,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : IDomainEvent;
