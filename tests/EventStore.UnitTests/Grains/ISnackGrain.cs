using System.Collections.Immutable;
using EventStore.UnitTests.Commands;
using EventStore.UnitTests.Events;
using EventStore.UnitTests.States;
using Orleans.Concurrency;
using Orleans.FluentResults;

namespace EventStore.UnitTests.Grains;

public interface ISnackGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task<Result<Snack>> GetAsync();

    [AlwaysInterleave]
    Task<Result<ImmutableList<SnackEvent>>> GetEventsAsync();

    [AlwaysInterleave]
    Task<bool> CanInitializeAsync();

    Task<Result<bool>> InitializeAsync(SnackInitializeCommand cmd);

    [AlwaysInterleave]
    Task<bool> CanRemoveAsync();

    Task<Result<bool>> RemoveAsync(SnackRemoveCommand cmd);

    [AlwaysInterleave]
    Task<bool> CanChangeNameAsync();

    Task<Result<bool>> ChangeNameAsync(SnackChangeNameCommand cmd);
}
