namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackRepoDeleteManyCommand
    (IList<Guid> SnackIds,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackRepoCommand(TraceId, OperatedAt, OperatedBy);
