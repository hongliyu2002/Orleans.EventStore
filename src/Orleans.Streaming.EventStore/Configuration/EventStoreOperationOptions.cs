using EventStore.Client;
using Orleans.Runtime;
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

    /// <summary>
    ///     The serializer used in serialize state to EventStore stream.
    /// </summary>
    public IGrainStorageSerializer GrainStorageSerializer { get; set; } = null!;

    #region Create Client

    /// <summary>
    ///     The delegate used to create a <see cref="EventStoreClient" /> instance.
    /// </summary>
    internal Func<EventStoreClient> CreateClient { get; private set; } = null!;

    /// <summary>
    ///     Configures the <see cref="EventStoreClient" /> using a connection string.
    /// </summary>
    public void ConfigureEventStoreClient(string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));
        CreateClient = () => new EventStoreClient(EventStoreClientSettings.Create(connectionString));
    }

    /// <summary>
    ///     Configures the <see cref="EventStoreClient" /> using the provided callback.
    /// </summary>
    public void ConfigureEventStoreClient(Func<EventStoreClient> createClient)
    {
        ArgumentNullException.ThrowIfNull(createClient, nameof(createClient));
        CreateClient = createClient;
    }

    internal void Validate(string? name)
    {
        if (CreateClient is null)
        {
            throw new OrleansConfigurationException($"No credentials specified. Use the {GetType().Name}.{nameof(ConfigureEventStoreClient)} method to configure the Event Store client.");
        }
    }

    #endregion

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
        Options.Validate(Name);
    }
}
