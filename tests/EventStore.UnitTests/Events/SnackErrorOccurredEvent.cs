using System.Collections.Immutable;

namespace EventStore.UnitTests.Events;

public sealed record SnackErrorOccurredEvent(Guid Id, int Code, IImmutableList<string> Reasons, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) : ErrorOccurredEvent(Code, Reasons, TraceId, OperatedAt, OperatedBy, Version);
