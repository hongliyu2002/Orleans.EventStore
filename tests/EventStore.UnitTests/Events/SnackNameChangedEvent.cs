namespace EventStore.UnitTests.Events;

public sealed record SnackNameChangedEvent(Guid Id, string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) : SnackEvent(Id, TraceId, OperatedAt, OperatedBy, Version);
