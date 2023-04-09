namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackTotalAmountUpdatedEvent
    (Guid SnackId,
     decimal TotalAmount,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, 0, TraceId, OperatedAt, OperatedBy);
