namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackDeleteCommand
    (Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);
