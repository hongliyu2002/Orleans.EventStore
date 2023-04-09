using SiloX.Domain.Abstractions;

namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public abstract record SnackCommand
    (Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : DomainCommand(TraceId, OperatedAt, OperatedBy);
