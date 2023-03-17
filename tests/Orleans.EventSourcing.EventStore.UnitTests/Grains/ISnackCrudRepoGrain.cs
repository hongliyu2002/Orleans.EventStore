using System.Collections.Immutable;
using Orleans.EventSourcing.EventStore.UnitTests.Commands;
using Orleans.Concurrency;
using Orleans.FluentResults;

namespace Orleans.EventSourcing.EventStore.UnitTests.Grains;

public interface ISnackCrudRepoGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task<Result<ISnackGrain>> GetAsync(SnackRepoGetCommand command);

    [AlwaysInterleave]
    Task<Result<ImmutableList<ISnackGrain>>> GetManyAsync(SnackRepoGetManyCommand command);

    Task<Result<bool>> CreateAsync(SnackRepoCreateCommand cmd);

    Task<Result<bool>> DeleteAsync(SnackRepoDeleteCommand cmd);
}
