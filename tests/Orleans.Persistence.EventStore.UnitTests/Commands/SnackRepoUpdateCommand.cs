namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackRepoUpdateCommand
    (Guid SnackId,
     string Name,
     string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackRepoCommand(TraceId, OperatedAt, OperatedBy);
