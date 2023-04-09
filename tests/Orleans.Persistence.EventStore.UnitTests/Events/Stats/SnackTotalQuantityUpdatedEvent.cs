namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackTotalQuantityUpdatedEvent
    (Guid SnackId,
     int TotalQuantity,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, 0, TraceId, OperatedAt, OperatedBy);
