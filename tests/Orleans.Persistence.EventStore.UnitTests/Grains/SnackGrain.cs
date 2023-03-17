using Fluxera.Guards;
using Microsoft.Extensions.Logging;
using Orleans.FluentResults;
using Orleans.Persistence.EventStore.UnitTests.Commands;
using Orleans.Runtime;

namespace Orleans.Persistence.EventStore.UnitTests.Grains;

public class SnackGrain : Grain, ISnackGrain
{
    private readonly IPersistentState<Snack> _snack;
    private readonly ILogger<SnackGrain> _logger;

    /// <inheritdoc />
    public SnackGrain([PersistentState("Snack", Constants.TestStoreName)] IPersistentState<Snack> snack, ILogger<SnackGrain> logger)
    {
        _snack = Guard.Against.Null(snack, nameof(snack));
        _logger = Guard.Against.Null(logger, nameof(logger));
    }

    /// <inheritdoc />
    public Task<Result<Snack>> GetAsync()
    {
        var id = this.GetPrimaryKey();
        return Task.FromResult(Result.Ok(_snack.State).Ensure(_snack.State.IsCreated, $"Snack {id} is not initialized."));
    }

    /// <inheritdoc />
    public Task<bool> CanInitializeAsync()
    {
        return Task.FromResult(_snack.State.IsDeleted == false && _snack.State.IsCreated == false);
    }

    /// <inheritdoc />
    public Task<Result<bool>> InitializeAsync(SnackInitializeCommand cmd)
    {
        var id = this.GetPrimaryKey();
        return Result.Ok()
                     .Ensure(_snack.State.IsDeleted == false, $"Snack {id} has already been removed.")
                     .Ensure(_snack.State.IsCreated == false, $"Snack {id} already exists.")
                     .Ensure(_snack.State.Name.Length <= 100, $"The name of snack {id} is too long.")
                     .Tap(() =>
                          {
                              _snack.State.Id = id;
                              _snack.State.Name = cmd.Name;
                              _snack.State.CreatedAt = cmd.OperatedAt;
                              _snack.State.CreatedBy = cmd.OperatedBy;
                          })
                     .TapTryAsync(() => _snack.WriteStateAsync())
                     .MapAsync(() => true);
    }

    /// <inheritdoc />
    public Task<bool> CanRemoveAsync()
    {
        return Task.FromResult(_snack.State.IsDeleted == false && _snack.State.IsCreated);
    }

    /// <inheritdoc />
    public Task<Result<bool>> RemoveAsync(SnackRemoveCommand cmd)
    {
        var id = this.GetPrimaryKey();
        return Result.Ok()
                     .Ensure(_snack.State.IsDeleted == false, $"Snack {id} has already been removed.")
                     .Ensure(_snack.State.IsCreated, $"Snack {id} is not initialized.")
                     .Tap(() =>
                          {
                              _snack.State.DeletedAt = cmd.OperatedAt;
                              _snack.State.DeletedBy = cmd.OperatedBy;
                              _snack.State.IsDeleted = true;
                          })
                     .TapTryAsync(() => _snack.WriteStateAsync())
                     .MapAsync(() => true);
    }

    /// <inheritdoc />
    public Task<bool> CanChangeNameAsync()
    {
        return Task.FromResult(_snack.State.IsDeleted == false && _snack.State.IsCreated);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ChangeNameAsync(SnackChangeNameCommand cmd)
    {
        var id = this.GetPrimaryKey();
        return Result.Ok()
                     .Ensure(_snack.State.IsDeleted == false, $"Snack {id} has already been removed.")
                     .Ensure(_snack.State.IsCreated, $"Snack {id} is not initialized.")
                     .Ensure(_snack.State.Name.Length <= 100, $"The name of snack {id} is too long.")
                     .Tap(() =>
                          {
                              _snack.State.Name = cmd.Name;
                              _snack.State.LastModifiedAt = cmd.OperatedAt;
                              _snack.State.LastModifiedBy = cmd.OperatedBy;
                          })
                     .TapTryAsync(() => _snack.WriteStateAsync())
                     .MapAsync(() => true);
    }
}
