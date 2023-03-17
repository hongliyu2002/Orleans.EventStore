using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Orleans.Storage;

/// <summary>
///     Factory used to create instances of EventStore grain storage.
/// </summary>
public static class EventStoreGrainStorageFactory
{
    /// <summary>
    ///     Creates a EventStore grain storage instance.
    /// </summary>
    public static EventStoreGrainStorage Create(IServiceProvider serviceProvider, string name)
    {
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>();
        return ActivatorUtilities.CreateInstance<EventStoreGrainStorage>(serviceProvider, name, options.Get(name));
    }
}
