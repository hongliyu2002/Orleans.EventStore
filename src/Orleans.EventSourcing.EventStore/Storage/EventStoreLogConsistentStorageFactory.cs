using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.EventSourcing.Configuration;

namespace Orleans.EventSourcing.EventStoreStorage;

/// <summary>
///     Factory used to create instances of EventStore log consistent storage.
/// </summary>
public static class EventStoreLogConsistentStorageFactory
{
    /// <summary>
    ///     Creates a EventStore log consistent storage instance.
    /// </summary>
    public static ILogConsistentStorage Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>();
        return ActivatorUtilities.CreateInstance<EventStoreLogConsistentStorage>(services, name, optionsMonitor.Get(name));
    }
}
