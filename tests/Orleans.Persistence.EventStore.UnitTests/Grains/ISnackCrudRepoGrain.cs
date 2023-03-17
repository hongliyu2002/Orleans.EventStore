using System.Collections.Immutable;
using Orleans.Concurrency;
using Orleans.FluentResults;
using Orleans.Persistence.EventStore.UnitTests.Commands;

namespace Orleans.Persistence.EventStore.UnitTests.Grains;

public interface ISnackCrudRepoGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task<Result<ISnackGrain>> GetAsync(SnackRepoGetCommand command);

    [AlwaysInterleave]
    Task<Result<ImmutableList<ISnackGrain>>> GetManyAsync(SnackRepoGetManyCommand command);

    Task<Result<bool>> CreateAsync(SnackRepoCreateCommand cmd);

    Task<Result<bool>> DeleteAsync(SnackRepoDeleteCommand cmd);
}
