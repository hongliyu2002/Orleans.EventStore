using System.Collections.Immutable;
using EventStore.UnitTests.Commands;
using Orleans.Concurrency;
using Orleans.FluentResults;

namespace EventStore.UnitTests.Grains;

public interface ISnackCrudRepoGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task<Result<ISnackGrain>> GetAsync(SnackRepoGetCommand command);

    [AlwaysInterleave]
    Task<Result<ImmutableList<ISnackGrain>>> GetManyAsync(SnackRepoGetManyCommand command);

    Task<Result<bool>> CreateAsync(SnackRepoCreateCommand cmd);

    Task<Result<bool>> DeleteAsync(SnackRepoDeleteCommand cmd);
}
