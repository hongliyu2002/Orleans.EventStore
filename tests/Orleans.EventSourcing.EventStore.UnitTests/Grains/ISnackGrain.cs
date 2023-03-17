using System.Collections.Immutable;
using Orleans.EventSourcing.EventStore.UnitTests.Commands;
using Orleans.EventSourcing.EventStore.UnitTests.Events;
using Orleans.EventSourcing.EventStore.UnitTests.States;
using Orleans.Concurrency;
using Orleans.FluentResults;

namespace Orleans.EventSourcing.EventStore.UnitTests.Grains;

public interface ISnackGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task<Result<Snack>> GetAsync();

    [AlwaysInterleave]
    Task<Result<ImmutableList<SnackEvent>>> GetEventsAsync(int fromVersion, int toVersion);

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
