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
[Serializable]
[GenerateSerializer]
public abstract record SnackCommand
    (Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : DomainCommand(TraceId, OperatedAt, OperatedBy);


[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackInitializeCommand
    (Guid SnackId,
     string Name,
     string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackChangeNameCommand
    (string Name,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackChangePictureUrlCommand
    (string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);
 
[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackRemoveCommand
    (Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackCommand(TraceId, OperatedAt, OperatedBy);
```
#### Events:
```csharp
[Immutable]
[Serializable]
[GenerateSerializer]
public abstract record SnackEvent
    (Guid SnackId,
     int Version,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : DomainEvent(Version, TraceId, OperatedAt, OperatedBy);

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackInitializedEvent
    (Guid SnackId,
     int Version,
     string Name,
     string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, Version, TraceId, OperatedAt, OperatedBy);

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackNameChangedEvent
    (Guid SnackId,
     int Version,
     string Name,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, Version, TraceId, OperatedAt, OperatedBy);

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackPictureUrlChangedEvent
    (Guid SnackId,
     int Version,
     string? PictureUrl,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, Version, TraceId, OperatedAt, OperatedBy);

[Immutable]
[Serializable]
[GenerateSerializer]
public sealed record SnackRemovedEvent
    (Guid SnackId,
     int Version,
     Guid TraceId,
     DateTimeOffset OperatedAt,
     string OperatedBy) : SnackEvent(SnackId, Version, TraceId, OperatedAt, OperatedBy);
```
*Only by applying events can Grain State objects be modified, which is the basis of Event Sourcing.*
#### Grain State:
```csharp
[Serializable]
[GenerateSerializer]
public sealed class Snack : IAuditedObject, ISoftDeleteObject
{
    /// <summary>
    ///     The unique identifier of the snack.
    /// </summary>
    [Id(0)]
    public Guid Id { get; set; }

    /// <summary>
    ///     The date and time when the snack was created.
    /// </summary>
    [Id(1)]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    ///     The user who created the snack.
    /// </summary>
    [Id(2)]
    public string? CreatedBy { get; set; }

    /// <summary>
    ///     Indicates whether the snack has been created.
    /// </summary>
    public bool IsCreated => CreatedAt != null;

    /// <summary>
    ///     The date and time when the snack was last modified.
    /// </summary>
    [Id(3)]
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    ///     The user who last modified the snack.
    /// </summary>
    [Id(4)]
    public string? LastModifiedBy { get; set; }

    /// <summary>
    ///     The date and time when the snack was deleted.
    /// </summary>
    [Id(5)]
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    ///     The user who deleted the snack.
    /// </summary>
    [Id(6)]
    public string? DeletedBy { get; set; }

    /// <summary>
    ///     Indicates whether the snack has been deleted.
    /// </summary>
    [Id(7)]
    public bool IsDeleted { get; set; }

    /// <summary>
    ///     The name of the snack.
    /// </summary>
    [Id(8)]
    public string Name { get; set; } = null!;

    /// <summary>
    ///     The URL of the picture of the snack.
    /// </summary>
    [Id(9)]
    public string? PictureUrl { get; set; }

    public override string ToString()
    {
        return $"Snack with Id:'{Id}' Name:'{Name}' PictureUrl:'{PictureUrl}'";
    }

    #region Apply

    public void Apply(SnackInitializeCommand command)
    {
        Id = command.SnackId;
        Name = command.Name;
        PictureUrl = command.PictureUrl;
        CreatedAt = command.OperatedAt;
        CreatedBy = command.OperatedBy;
    }

    public void Apply(SnackRemoveCommand command)
    {
        DeletedAt = command.OperatedAt;
        DeletedBy = command.OperatedBy;
        IsDeleted = true;
    }

    public void Apply(SnackChangeNameCommand command)
    {
        Name = command.Name;
        LastModifiedAt = command.OperatedAt;
        LastModifiedBy = command.OperatedBy;
    }

    public void Apply(SnackChangePictureUrlCommand command)
    {
        PictureUrl = command.PictureUrl;
        LastModifiedAt = command.OperatedAt;
        LastModifiedBy = command.OperatedBy;
    }

    #endregion

}
```
#### Grain Interface:
```csharp
public interface ISnackGrain : IGrainWithGuidKey
{
    /// <summary>
    ///     Asynchronously retrieves the current state of the Snack
    /// </summary>
    [AlwaysInterleave]
    Task<Snack> GetStateAsync();

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
    Task<bool> CanRemoveAsync(SnackRemoveCommand command);

    /// <summary>
    ///     Asynchronously removes the Snack with the given command
    /// </summary>
    Task<Result> RemoveAsync(SnackRemoveCommand command);

    /// <summary>
    ///     Asynchronously checks whether the Snack's name can be changed with the given command
    /// </summary>
    [AlwaysInterleave]
    Task<bool> CanChangeNameAsync(SnackChangeNameCommand command);

    /// <summary>
    ///     Asynchronously changes the Snack's name with the given command
    /// </summary>
    Task<Result> ChangeNameAsync(SnackChangeNameCommand command);

    /// <summary>
    ///     Asynchronously checks whether the Snack's picture URL can be changed with the given command
    /// </summary>
    [AlwaysInterleave]
    Task<bool> CanChangePictureUrlAsync(SnackChangePictureUrlCommand command);

    /// <summary>
    ///     Asynchronously changes the Snack's picture URL with the given command
    /// </summary>
    Task<Result> ChangePictureUrlAsync(SnackChangePictureUrlCommand command);
}
```
#### Grain:
```csharp
[LogConsistencyProvider(ProviderName = Constants.LogConsistencyName)]
[StorageProvider(ProviderName = Constants.GrainStorageName)]
public sealed class SnackGrain : EventSourcingGrainWithGuidKey<Snack, SnackCommand, SnackEvent, SnackErrorEvent>, ISnackGrain
{
    private readonly DomainDbContext _dbContext;

    /// <inheritdoc />
    public SnackGrain(DomainDbContext dbContext)
        : base(Constants.StreamProviderName)
    {
        _dbContext = Guard.Against.Null(dbContext, nameof(dbContext));
    }

    /// <inheritdoc />
    protected override string GetStreamNamespace()
    {
        return Constants.SnacksNamespace;
    }

    /// <inheritdoc />
    protected override string GetBroadcastStreamNamespace()
    {
        return Constants.SnacksBroadcastNamespace;
    }

    /// <inheritdoc />
    public Task<Snack> GetStateAsync()
    {
        return Task.FromResult(State);
    }

    /// <inheritdoc />
    public Task<int> GetVersionAsync()
    {
        return Task.FromResult(Version);
    }

    private Result ValidateInitialize(SnackInitializeCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok()
                     .Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.")
                     .Verify(State.IsCreated == false, $"Snack {snackId} already exists.")
                     .Verify(command.Name.IsNotNullOrWhiteSpace(), $"The name of snack {snackId} should not be empty.")
                     .Verify(command.Name.Length <= 100, $"The name of snack {snackId} is too long.")
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
        return ValidateInitialize(command)
              .MapTryAsync(() => RaiseConditionalEvent(command))
              .MapTryIfAsync(persisted => persisted, PersistAsync)
              .MapTryAsync(() => PublishAsync(new SnackInitializedEvent(State.Id, Version, State.Name, State.PictureUrl, command.TraceId, State.CreatedAt ?? DateTimeOffset.UtcNow, State.CreatedBy ?? command.OperatedBy)))
              .TapErrorTryAsync(errors => PublishErrorAsync(new SnackErrorEvent(this.GetPrimaryKey(), Version, 101, errors.ToReasonStrings(), command.TraceId, DateTimeOffset.UtcNow, command.OperatedBy)));
    }

    private Result ValidateRemove(SnackRemoveCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok().Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.").Verify(State.IsCreated, $"Snack {snackId} is not initialized.").Verify(command.OperatedBy.IsNotNullOrWhiteSpace(), "Operator should not be empty.");
    }

    /// <inheritdoc />
    public Task<bool> CanRemoveAsync(SnackRemoveCommand command)
    {
        return Task.FromResult(ValidateRemove(command).IsSuccess);
    }

    /// <inheritdoc />
    public Task<Result> RemoveAsync(SnackRemoveCommand command)
    {
        return ValidateRemove(command)
              .MapTryAsync(() => RaiseConditionalEvent(command))
              .MapTryIfAsync(persisted => persisted, PersistAsync)
              .MapTryAsync(() => PublishAsync(new SnackRemovedEvent(State.Id, Version, command.TraceId, State.DeletedAt ?? DateTimeOffset.UtcNow, State.DeletedBy ?? command.OperatedBy)))
              .TapErrorTryAsync(errors => PublishErrorAsync(new SnackErrorEvent(this.GetPrimaryKey(), Version, 102, errors.ToReasonStrings(), command.TraceId, DateTimeOffset.UtcNow, command.OperatedBy)));
    }

    private Result ValidateChangeName(SnackChangeNameCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok()
                     .Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.")
                     .Verify(State.IsCreated, $"Snack {snackId} is not initialized.")
                     .Verify(command.Name.IsNotNullOrWhiteSpace(), $"The name of snack {snackId} should not be empty.")
                     .Verify(command.Name.Length <= 100, $"The name of snack {snackId} is too long.")
                     .Verify(command.OperatedBy.IsNotNullOrWhiteSpace(), "Operator should not be empty.");
    }

    /// <inheritdoc />
    public Task<bool> CanChangeNameAsync(SnackChangeNameCommand command)
    {
        return Task.FromResult(ValidateChangeName(command).IsSuccess);
    }

    /// <inheritdoc />
    public Task<Result> ChangeNameAsync(SnackChangeNameCommand command)
    {
        return ValidateChangeName(command)
              .MapTryAsync(() => RaiseConditionalEvent(command))
              .MapTryIfAsync(persisted => persisted, PersistAsync)
              .MapTryAsync(() => PublishAsync(new SnackNameChangedEvent(State.Id, Version, State.Name, command.TraceId, State.LastModifiedAt ?? DateTimeOffset.UtcNow, State.LastModifiedBy ?? command.OperatedBy)))
              .TapErrorTryAsync(errors => PublishErrorAsync(new SnackErrorEvent(this.GetPrimaryKey(), Version, 103, errors.ToReasonStrings(), command.TraceId, DateTimeOffset.UtcNow, command.OperatedBy)));
    }

    private Result ValidateChangePictureUrl(SnackChangePictureUrlCommand command)
    {
        var snackId = this.GetPrimaryKey();
        return Result.Ok()
                     .Verify(State.IsDeleted == false, $"Snack {snackId} has already been removed.")
                     .Verify(State.IsCreated, $"Snack {snackId} is not initialized.")
                     .Verify(command.PictureUrl.IsNullOrWhiteSpace() || command.PictureUrl!.Length <= 500, $"The picture url of snack {snackId} is too long.")
                     .Verify(command.OperatedBy.IsNotNullOrWhiteSpace(), "Operator should not be empty.");
    }

    /// <inheritdoc />
    public Task<bool> CanChangePictureUrlAsync(SnackChangePictureUrlCommand command)
    {
        return Task.FromResult(ValidateChangePictureUrl(command).IsSuccess);
    }

    /// <inheritdoc />
    public Task<Result> ChangePictureUrlAsync(SnackChangePictureUrlCommand command)
    {
        return ValidateChangePictureUrl(command)
              .MapTryAsync(() => RaiseConditionalEvent(command))
              .MapTryAsync(() => PublishAsync(new SnackPictureUrlChangedEvent(State.Id, Version, State.PictureUrl, command.TraceId, State.LastModifiedAt ?? DateTimeOffset.UtcNow, State.LastModifiedBy ?? command.OperatedBy)))
              .TapErrorTryAsync(errors => PublishErrorAsync(new SnackErrorEvent(this.GetPrimaryKey(), Version, 104, errors.ToReasonStrings(), command.TraceId, DateTimeOffset.UtcNow, command.OperatedBy)));
    }
}
```
