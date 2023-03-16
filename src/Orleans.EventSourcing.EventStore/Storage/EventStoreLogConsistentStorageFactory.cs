using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.EventSourcing.EventStore.Configuration;

namespace Orleans.EventSourcing.EventStore;

/// <summary>
///     Factory used to create instances of EventStore log consistent storage.
/// </summary>
public static class EventStoreLogConsistentStorageFactory
{
    /// <summary>
    ///     Creates a EventStore log consistent storage instance.
    /// </summary>
    public static ILogConsistentStorage Create(IServiceProvider serviceProvider, string name)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>();
        return ActivatorUtilities.CreateInstance<EventStoreLogConsistentStorage>(serviceProvider, name, optionsMonitor.Get(name));
    }
}
