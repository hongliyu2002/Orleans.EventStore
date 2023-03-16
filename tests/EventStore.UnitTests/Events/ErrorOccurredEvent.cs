using System.Collections.Immutable;

namespace EventStore.UnitTests.Events;

[Immutable]
[GenerateSerializer]
public abstract record ErrorOccurredEvent(int Code, IImmutableList<string> Reasons, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : DomainEvent(TraceId, OperatedAt, OperatedBy, Version);
