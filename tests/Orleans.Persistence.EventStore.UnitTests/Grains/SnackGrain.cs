using Fluxera.Utilities.Extensions;
using Orleans.FluentResults;
using Orleans.Providers;
using Vending.Domain.Abstractions;
using Vending.Domain.Abstractions.Snacks;

namespace Vending.Domain.Snacks;

[StorageProvider(ProviderName = Constants.GrainStorageName)]
public sealed class SnackGrain : Grain<Snack>, ISnackGrain
{
    /// <inheritdoc />
    public Task<Snack> GetSnackAsync()
    {
        return Task.FromResult(State);
    }

    /// <inheritdoc />
    public Task<int> GetVersionAsync()
    {
        return Task.FromResult(0);
    }

    private Result ValidateInitialize(SnackInitializeCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok()
                     .Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.")
                     .Verify(State.IsCreated == false, $"Snack {snackId} already exists.")
                     .Verify(command.Name.IsNotNullOrWhiteSpace(), $"The name of snack {snackId} should not be empty.")
                     .Verify(command.Name.Length <= 200, $"The name of snack {snackId} is too long.")
                     .Verify(command.PictureUrl == null || command.PictureUrl!.Length <= 500, $"The picture url of snack {snackId} is too long.")
                     .Verify(command.OperatedBy.IsNotNullOrWhiteSpace(), "Operator should not be empty.");
    }

    /// <inheritdoc />
    public Task<bool> CanInitializeAsync(SnackInitializeCommand command)
    {
        return Task.FromResult(ValidateInitialize(command).IsSuccess);
    }

    /// <inheritdoc />
    public Task<Result> InitializeAsync(SnackInitializeCommand command)
    {
        return ValidateInitialize(command).TapTry(() => State.Apply(command)).TapTryAsync(WriteStateAsync);
    }

    private Result ValidateRemove(SnackDeleteCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok().Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.").Verify(State.IsCreated, $"Snack {snackId} is not initialized.").Verify(command.OperatedBy.IsNotNullOrWhiteSpace(), "Operator should not be empty.");
    }

    /// <inheritdoc />
    public Task<bool> CanDeleteAsync(SnackDeleteCommand command)
    {
        return Task.FromResult(ValidateRemove(command).IsSuccess);
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(SnackDeleteCommand command)
    {
        return ValidateRemove(command).TapTry(() => State.Apply(command)).TapTryAsync(WriteStateAsync);
    }

    private Result ValidateUpdate(SnackUpdateCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok()
                     .Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.")
                     .Verify(State.IsCreated, $"Snack {snackId} is not initialized.")
                     .Verify(command.Name.IsNotNullOrWhiteSpace(), $"The name of snack {snackId} should not be empty.")
                     .Verify(command.Name.Length <= 200, $"The name of snack {snackId} is too long.")
                     .Verify(command.PictureUrl.IsNullOrWhiteSpace() || command.PictureUrl!.Length <= 500, $"The picture url of snack {snackId} is too long.")
                     .Verify(command.OperatedBy.IsNotNullOrWhiteSpace(), "Operator should not be empty.");
    }

    /// <inheritdoc />
    public Task<bool> CanUpdateAsync(SnackUpdateCommand command)
    {
        return Task.FromResult(ValidateUpdate(command).IsSuccess);
    }

    /// <inheritdoc />
    public Task<Result> UpdateAsync(SnackUpdateCommand command)
    {
        return ValidateUpdate(command).TapTry(() => State.Apply(command)).TapTryAsync(WriteStateAsync);
    }
}