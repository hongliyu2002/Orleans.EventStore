using System.Collections.Immutable;

namespace Orleans.EventSourcing.EventStore.UnitTests.Events;

[Immutable]
[GenerateSerializer]
public sealed record SnackErrorOccurredEvent(Guid Id, int Code, IImmutableList<string> Reasons, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : ErrorOccurredEvent(Code, Reasons, TraceId, OperatedAt, OperatedBy, Version);
