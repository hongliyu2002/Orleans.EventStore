﻿namespace EventStore.UnitTests.Events;

[Immutable]
[GenerateSerializer]
public sealed record SnackRemovedEvent(Guid Id, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : SnackEvent(Id, TraceId, OperatedAt, OperatedBy, Version);
