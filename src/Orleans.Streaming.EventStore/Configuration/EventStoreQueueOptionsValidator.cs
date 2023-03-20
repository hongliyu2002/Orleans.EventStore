using Orleans.Providers.Streams.EventStore;
using Orleans.Runtime;

namespace Orleans.Configuration;

/// <summary>
///     Configuration validator for EventStoreQueueOptions
/// </summary>
public class EventStoreQueueOptionsValidator : IConfigurationValidator
{
    private readonly EventStoreQueueOptions _options;
    private readonly string _name;

    /// <summary>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IConfigurationValidator Create(IServiceProvider services, string name)
    {
        var queueOptions = services.GetOptionsByName<EventStoreQueueOptions>(name);
        return new EventStoreQueueOptionsValidator(queueOptions, name);
    }

    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="name"></param>
    /// <exception cref="OrleansConfigurationException"></exception>
    public EventStoreQueueOptionsValidator(EventStoreQueueOptions options, string name)
    {
        _options = options ?? throw new OrleansConfigurationException($"Invalid EventStoreQueueOptions for EventStoreQueueStorage {name}. Options is required.");
        _name = name;
    }

    /// <inheritdoc />
    public void ValidateConfiguration()
    {
        if (_options.ClientSettings == null)
        {
            throw new OrleansConfigurationException($"Invalid configuration for {nameof(EventStoreQueueStorage)} with name {_name}. {nameof(EventStoreQueueOptions)}.{nameof(_options.ClientSettings)} is required.");
        }
        if (_options.SubscriptionSettings == null)
        {
            throw new OrleansConfigurationException($"Invalid configuration for {nameof(EventStoreQueueStorage)} with name {_name}. {nameof(EventStoreQueueOptions)}.{nameof(_options.SubscriptionSettings)} is required.");
        }
        if (_options.QueueNames == null || _options.QueueNames.Count == 0)
        {
            throw new OrleansConfigurationException($"{nameof(EventStoreQueueOptions)} on stream provider {_name} is invalid. {nameof(EventStoreQueueOptions.QueueNames)} is invalid");
        }
    }
}
