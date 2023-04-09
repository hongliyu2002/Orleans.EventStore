namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public abstract record SnackRepoCommand
    (Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);
