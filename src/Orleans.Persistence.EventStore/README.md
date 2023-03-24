# EventStore Provider for Microsoft Orleans Grain Storage

### Silo Configuration
```csharp
var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
silo.AddEventStoreGrainStorage(Constants.StateStoreName, 
        options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
        })
```
*Using EventStore DB as a Grain Storage Provider has an interesting feature: all data of the grain state changes are kept in EventStore, just like some time-series databases. By subscribing to the stream of this state, the data can be dynamically updated to the state database, such as SQL Server, making it easy to implement certain CQRS functionality.*

### Grain samples:
*Commands are immutable objects that can greatly improve efficiency in Orleans.*
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
#### Grain State:
```csharp
[GenerateSerializer]
public sealed class Snack
{
    public Snack(Guid id, string name, string? pictureUrl = null)
    {
        Id = Guard.Against.Empty(id, nameof(id));
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        PictureUrl = pictureUrl;
    }

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
}
```
#### Grain Interface:
```csharp
public interface ISnackGrain : IGrainWithGuidKey
{
    [AlwaysInterleave]
    Task<Result<Snack>> GetAsync();

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
public class SnackGrain : Grain, ISnackGrain
{
    private readonly IPersistentState<Snack> _snack;
    private readonly ILogger<SnackGrain> _logger;

    /// <inheritdoc />
    public SnackGrain([PersistentState("Snack", Constants.StateStoreName)] IPersistentState<Snack> snack, ILogger<SnackGrain> logger)
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
```
