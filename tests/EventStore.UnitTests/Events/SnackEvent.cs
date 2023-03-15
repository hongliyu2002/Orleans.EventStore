namespace EventStore.UnitTests.Events;

public abstract record SnackEvent(Guid Id, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) : DomainEvent(TraceId, OperatedAt, OperatedBy, Version);
