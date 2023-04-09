using SiloX.Domain.Abstractions;

namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackErrorEvent
    (Guid SnackId,
     int Version,
     int Code,
     IList<string> Reasons,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, Version, TraceId, OperatedAt, OperatedBy), IDomainErrorEvent;
