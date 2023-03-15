namespace EventStore.UnitTests.Events;

public abstract record DomainEvent(Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version);
