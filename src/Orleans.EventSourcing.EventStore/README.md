<img src="https://raw.githubusercontent.com/hongliyu2002/Orleans.FluentResult/master/resources/icons/logo_128.png" alt="Fluent Result"/>

# EventStore Provider for Microsoft Orleans EventSourcing

### Silo Configuration
```csharp
var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
silo.AddEventStoreBasedLogConsistencyProvider(Constants.LogConsistencyStoreName, 
        options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
        })
.AddMemoryGrainStorage(Constants.LogSnapshotStoreName);
```

### Event sourcing samples:
*Commands and events are immutable objects that can greatly improve efficiency in Orleans.*
#### Commands:
```csharp
[Immutable]
[GenerateSerializer]
public sealed record SnackInitializeCommand(string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);

[Immutable]
[GenerateSerializer]
public sealed record SnackChangeNameCommand(string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);

[Immutable]
[GenerateSerializer]
public sealed record SnackRemoveCommand(Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy) 
    : DomainCommand(TraceId, OperatedAt, OperatedBy);
```
#### Events:
```csharp
[Immutable]
[GenerateSerializer]
public sealed record SnackInitializedEvent(Guid Id, string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : SnackEvent(Id, TraceId, OperatedAt, OperatedBy, Version);

[Immutable]
[GenerateSerializer]
public sealed record SnackNameChangedEvent(Guid Id, string Name, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : SnackEvent(Id, TraceId, OperatedAt, OperatedBy, Version);
    
[Immutable]
[GenerateSerializer]
public sealed record SnackRemovedEvent(Guid Id, Guid TraceId, DateTimeOffset OperatedAt, string OperatedBy, int Version) 
    : SnackEvent(Id, TraceId, OperatedAt, OperatedBy, Version);
```
*Only by applying events can Grain State objects be modified, which is the basis of Event Sourcing.*
#### Grain State:
```csharp
[GenerateSerializer]
public sealed class Snack
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public DateTimeOffset? CreatedAt { get; set; }

    [Id(2)]
    public string? CreatedBy { get; set; }

    public bool IsCreated => CreatedAt != null;

    [Id(3)]
    public DateTimeOffset? LastModifiedAt { get; set; }

    [Id(4)]
    public string? LastModifiedBy { get; set; }

    [Id(5)]
    public DateTimeOffset? DeletedAt { get; set; }

    [Id(6)]
    public string? DeletedBy { get; set; }

    [Id(7)]
    public bool IsDeleted { get; set; }

    [Id(8)]
    public string Name { get; set; } = string.Empty;

    [Id(9)]
    public string? PictureUrl { get; set; }

    public override string ToString()
    {
        return $"Snack with Id:{Id} Name:'{Name}'";
    }

    #region Apply

    public void Apply(SnackInitializedEvent evt)
    {
        Id = evt.Id;
        Name = evt.Name;
        CreatedAt = evt.OperatedAt;
        CreatedBy = evt.OperatedBy;
    }

    public void Apply(SnackRemovedEvent evt)
    {
        DeletedAt = evt.OperatedAt;
        DeletedBy = evt.OperatedBy;
        IsDeleted = true;
    }

    public void Apply(SnackNameChangedEvent evt)
    {
        Name = evt.Name;
        LastModifiedAt = evt.OperatedAt;
        LastModifiedBy = evt.OperatedBy;
    }

    #endregion
}
```
#### Grain Interface:
```csharp
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
```
#### Grain:
```csharp
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
```
