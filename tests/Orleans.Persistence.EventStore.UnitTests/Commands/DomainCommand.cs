namespace Orleans.Persistence.EventStore.UnitTests.Commands;

[Immutable]
[GenerateSerializer]
public abstract record DomainCommand(Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy);
