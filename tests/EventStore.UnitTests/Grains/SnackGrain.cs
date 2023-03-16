using System.Collections.Immutable;
using EventStore.UnitTests.Commands;
using EventStore.UnitTests.Events;
using EventStore.UnitTests.States;
using Fluxera.Guards;
using Microsoft.Extensions.Logging;
using Orleans.EventSourcing;
using Orleans.FluentResults;
using Orleans.Providers;

namespace EventStore.UnitTests.Grains;

[LogConsistencyProvider(ProviderName = Constants.LogConsistencyStoreName)]
[StorageProvider(ProviderName = Constants.LogSnapshotStoreName)]
public class SnackGrain : JournaledGrain<Snack, SnackEvent>, ISnackGrain
{
    private readonly ILogger<SnackGrain> _logger;

    /// <inheritdoc />
    public SnackGrain(ILogger<SnackGrain> logger)
    {
        _logger = Guard.Against.Null(logger, nameof(logger));
    }

    /// <inheritdoc />
    public Task<Result<Snack>> GetAsync()
    {
        var id = this.GetPrimaryKey();
        return Task.FromResult(Result.Ok(State).Ensure(State.IsCreated, $"Snack {id} is not initialized."));
    }

    /// <inheritdoc />
    public Task<Result<ImmutableList<SnackEvent>>> GetEventsAsync(int fromVersion, int toVersion)
    {
        return Result.Ok().MapTryAsync(() => RetrieveConfirmedEvents(fromVersion, toVersion)).MapTryAsync(list => list.ToImmutableList());
    }

    /// <inheritdoc />
    public Task<bool> CanInitializeAsync()
    {
        return Task.FromResult(State.IsDeleted == false && State.IsCreated == false);
    }

    /// <inheritdoc />
    public Task<Result<bool>> InitializeAsync(SnackInitializeCommand cmd)
    {
        var id = this.GetPrimaryKey();
        return Result.Ok()
                     .Ensure(State.IsDeleted == false, $"Snack {id} has already been removed.")
                     .Ensure(State.IsCreated == false, $"Snack {id} already exists.")
                     .Ensure(State.Name.Length <= 100, $"The name of snack {id} is too long.")
                     .BindTryAsync(() => PublishPersistedAsync(new SnackInitializedEvent(id, cmd.Name, cmd.TraceId, DateTimeOffset.UtcNow, cmd.OperatedBy, Version)));
    }

    /// <inheritdoc />
    public Task<bool> CanRemoveAsync()
    {
        return Task.FromResult(State.IsDeleted == false && State.IsCreated);
    }

    /// <inheritdoc />
    public Task<Result<bool>> RemoveAsync(SnackRemoveCommand cmd)
    {
        var id = this.GetPrimaryKey();
        return Result.Ok()
                     .Ensure(State.IsDeleted == false, $"Snack {id} has already been removed.")
                     .Ensure(State.IsCreated, $"Snack {id} is not initialized.")
                     .BindTryAsync(() => PublishPersistedAsync(new SnackRemovedEvent(id, cmd.TraceId, DateTimeOffset.UtcNow, cmd.OperatedBy, Version)));
    }

    /// <inheritdoc />
    public Task<bool> CanChangeNameAsync()
    {
        return Task.FromResult(State.IsDeleted == false && State.IsCreated);
    }

    /// <inheritdoc />
    public Task<Result<bool>> ChangeNameAsync(SnackChangeNameCommand cmd)
    {
        var id = this.GetPrimaryKey();
        return Result.Ok()
                     .Ensure(State.IsDeleted == false, $"Snack {id} has already been removed.")
                     .Ensure(State.IsCreated, $"Snack {id} is not initialized.")
                     .Ensure(State.Name.Length <= 100, $"The name of snack {id} is too long.")
                     .BindTryAsync(() => PublishPersistedAsync(new SnackNameChangedEvent(id, cmd.Name, cmd.TraceId, DateTimeOffset.UtcNow, cmd.OperatedBy, Version)));
    }

    protected Task<Result<bool>> PublishPersistedAsync(SnackEvent evt)
    {
        return Result.Ok().MapTryAsync(() => RaiseConditionalEvent(evt));
    }
}
