namespace Orleans.Persistence.EventStore.UnitTests.Commands;

[Immutable]
[GenerateSerializer]
public sealed record SnackInitializeCommand(string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);
