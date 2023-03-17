namespace Orleans.EventSourcing.EventStore.UnitTests.Commands;

[Immutable]
[GenerateSerializer]
public sealed record SnackChangeNameCommand(string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);
