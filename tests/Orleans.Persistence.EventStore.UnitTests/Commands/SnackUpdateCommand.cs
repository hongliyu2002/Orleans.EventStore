namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackUpdateCommand
    (string Name,
     string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);
