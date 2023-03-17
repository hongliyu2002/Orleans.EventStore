using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     Factory used to create instances of EventStore log consistent storage.
/// </summary>
public static class EventStoreLogConsistentStorageFactory
{
    /// <summary>
    ///     Creates a EventStore log consistent storage instance.
    /// </summary>
    public static EventStoreLogConsistentStorage Create(IServiceProvider serviceProvider, string name)
    {
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>();
        return ActivatorUtilities.CreateInstance<EventStoreLogConsistentStorage>(serviceProvider, name, options.Get(name));
    }
}
