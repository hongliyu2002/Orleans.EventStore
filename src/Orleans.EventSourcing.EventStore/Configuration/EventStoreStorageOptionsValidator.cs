using Orleans.EventSourcing.EventStoreStorage;
using Orleans.Runtime;

namespace Orleans.EventSourcing.Configuration;

/// <summary>
///     Configuration validator for EventStoreStorageOptions
/// </summary>
public class EventStoreStorageOptionsValidator : IConfigurationValidator
{
    private readonly EventStoreStorageOptions _options;
    private readonly string _name;

    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="name"></param>
    /// <exception cref="OrleansConfigurationException"></exception>
    public EventStoreStorageOptionsValidator(EventStoreStorageOptions options, string name)
    {
        _options = options ?? throw new OrleansConfigurationException($"Invalid EventStoreStorageOptions for EventStoreLogConsistentStorage {name}. Options is required.");
        _name = name;
    }

    /// <inheritdoc />
    public void ValidateConfiguration()
    {
        if (_options.ClientSettings == null)
        {
            throw new OrleansConfigurationException($"Invalid configuration for {nameof(EventStoreLogConsistentStorage)} with name {_name}. {nameof(EventStoreStorageOptions)}.{nameof(_options.ClientSettings)} is required.");
        }
    }
}
