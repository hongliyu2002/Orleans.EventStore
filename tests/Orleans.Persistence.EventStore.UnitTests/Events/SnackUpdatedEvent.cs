namespace Vending.Domain.Abstractions.Snacks;

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackUpdatedEvent
    (Guid SnackId,
     int Version,
     string Name,
     string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, Version, TraceId, OperatedAt, OperatedBy);
