namespace EventStore.UnitTests.Commands;

[Immutable]
[GenerateSerializer]
public sealed record SnackRemoveCommand(Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);
