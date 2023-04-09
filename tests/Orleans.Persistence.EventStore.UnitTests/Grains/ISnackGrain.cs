using Orleans.Concurrency;
using Orleans.FluentResults;

namespace Vending.Domain.Abstractions.Snacks;

/// <summary>
///     This interface defines the contract for the SnackGrain
/// </summary>
public interface ISnackGrain : IGrainWithGuidKey
{
    /// <summary>
    ///     Asynchronously retrieves the current state of the Snack
    /// </summary>
    [AlwaysInterleave]
    Task<Snack> GetSnackAsync();

    /// <summary>
    ///     Asynchronously retrieves the current version number of the Snack
    /// </summary>
    [AlwaysInterleave]
    Task<int> GetVersionAsync();

    /// <summary>
    ///     Asynchronously checks whether the Snack can be initialized with the given command
    /// </summary>
    [AlwaysInterleave]
    Task<bool> CanInitializeAsync(SnackInitializeCommand command);

    /// <summary>
    ///     Asynchronously initializes the Snack with the given command
    /// </summary>
    Task<Result> InitializeAsync(SnackInitializeCommand command);

    /// <summary>
    ///     Asynchronously checks whether the Snack can be removed with the given command
    /// </summary>
    [AlwaysInterleave]
    Task<bool> CanDeleteAsync(SnackDeleteCommand command);

    /// <summary>
    ///     Asynchronously removes the Snack with the given command
    /// </summary>
    Task<Result> DeleteAsync(SnackDeleteCommand command);

    /// <summary>
    ///     Asynchronously checks whether the snack's name and picture url can be changed with the given command
    /// </summary>
    [AlwaysInterleave]
    Task<bool> CanUpdateAsync(SnackUpdateCommand command);

    /// <summary>
    ///     Asynchronously changes the snack's name and picture url with the given command
    /// </summary>
    Task<Result> UpdateAsync(SnackUpdateCommand command);
}
