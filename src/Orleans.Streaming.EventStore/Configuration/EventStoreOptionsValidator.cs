using Orleans.Runtime;

namespace Orleans.Configuration;

/// <summary>
///  </summary>
public class EventStoreOptionsValidator : IConfigurationValidator
{
    private readonly EventStoreOptions _options;
    private readonly string _name;

    /// <summary>
    ///     Constructor for <see cref="EventStoreOptionsValidator" />
    /// </summary>
    /// <param name="options">The <see cref="EventStoreOptions" /> to validate.</param>
    /// <param name="name">The name of the stream provider.</param>
    public EventStoreOptionsValidator(EventStoreOptions options, string name)
    {
        _options = options;
        _name = name;
    }

    /// <summary>
    ///     Validates the EventStore configuration.
    /// </summary>
    /// <exception cref="OrleansConfigurationException">Thrown when the EventStore client settings are not configured, or when <see cref="EventStoreOptions.Name" /> is not set, or when <see cref="EventStoreOptions.Queues" /> is empty.</exception>
    public void ValidateConfiguration()
    {
        if (_options.ClientSettings is null)
        {
            throw new OrleansConfigurationException($"EventStore client settings not configured for stream provider _options {nameof(EventStoreOptions)} with _name \"{_name}\". ");
        }
        if (string.IsNullOrEmpty(_options.Name))
        {
            throw new OrleansConfigurationException($"{nameof(EventStoreOptions)} on stream provider {_name} is invalid. {nameof(EventStoreOptions.Name)} is invalid");
        }
        if (_options.Queues == null || _options.Queues.Count == 0)
        {
            throw new OrleansConfigurationException($"{nameof(EventStoreOptions)} on stream provider {_name} is invalid. {nameof(EventStoreOptions.Queues)} should not be empty.");
        }
    }
}
