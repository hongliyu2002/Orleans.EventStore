namespace EventStore.UnitTests.Events;

[Immutable]
[GenerateSerializer]
public sealed record SnackInitializedEvent(Guid Id, string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : SnackEvent(Id, TraceId, OperatedAt, OperatedBy, Version);
