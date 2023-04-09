namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackRepoDeleteCommand
    (Guid SnackId,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackRepoCommand(TraceId, OperatedAt, OperatedBy);
