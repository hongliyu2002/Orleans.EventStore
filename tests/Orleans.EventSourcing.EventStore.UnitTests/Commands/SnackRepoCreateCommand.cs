namespace Orleans.EventSourcing.EventStore.UnitTests.Commands;

[Immutable]
[GenerateSerializer]
public sealed record SnackRepoCreateCommand(Guid Id, string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);
