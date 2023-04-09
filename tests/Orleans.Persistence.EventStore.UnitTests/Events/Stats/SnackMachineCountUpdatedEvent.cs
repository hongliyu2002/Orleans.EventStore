namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackMachineCountUpdatedEvent
    (Guid SnackId,
     int MachineCount,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, 0, TraceId, OperatedAt, OperatedBy);
