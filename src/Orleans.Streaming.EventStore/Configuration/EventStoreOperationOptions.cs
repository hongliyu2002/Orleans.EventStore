using EventStore.Client;
using Orleans.Storage;

namespace Orleans.Configuration;

/// <summary>
/// </summary>
public class EventStoreOperationOptions
{
    /// <summary>
    ///     EventStore storage policy options. (Like operation timeout setting..)
    /// </summary>
    public EventStorePolicyOptions PolicyOptions { get; } = new();

    /// <summary>
    ///     Settings to be used when configuring the EventStore client.
    /// </summary>
    [Redact]
    public EventStoreClientSettings ClientSettings { get; set; } = null!;

    /// <summary>
    ///     The user credentials that have permissions to append events.
    /// </summary>
    [Redact]
    public UserCredentials? Credentials { get; set; }
}

/// <summary>
/// </summary>
/// <typeparam name="TOptions"></typeparam>
public class EventStoreOperationOptionsValidator<TOptions> : IConfigurationValidator
    where TOptions : EventStoreOperationOptions
{
    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="name"></param>
    public EventStoreOperationOptionsValidator(TOptions options, string? name = null)
    {
        Options = options;
        Name = name;
    }

    /// <summary>
    /// </summary>
    public TOptions Options { get; }

    /// <summary>
    /// </summary>
    public string? Name { get; }

    /// <inheritdoc />
    public void ValidateConfiguration()
    {
    }
}
