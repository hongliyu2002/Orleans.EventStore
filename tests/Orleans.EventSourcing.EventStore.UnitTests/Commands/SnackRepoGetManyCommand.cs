namespace Orleans.EventSourcing.EventStore.UnitTests.Commands;

[Immutable]
[GenerateSerializer]
public sealed record SnackRepoGetManyCommand(Guid[] Ids, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);
