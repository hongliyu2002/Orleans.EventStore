using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreStorageSiloBuilderExtensions
{
    /// <summary>
    ///     Configures EventStore as the default grain storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreGrainStorageAsDefault(this ISiloBuilder builder, Action<EventStoreStorageOptions> configureOptions)
    {
        return builder.AddEventStoreGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as a grain storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreGrainStorage(this ISiloBuilder builder, string name, Action<EventStoreStorageOptions> configureOptions)
    {
        return builder.ConfigureServices(services => services.AddEventStoreGrainStorage(name, configureOptions));
    }

    /// <summary>
    ///     Configures EventStore as the default grain storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        return builder.AddEventStoreGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as a grain storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        return builder.ConfigureServices(services => services.AddEventStoreGrainStorage(name, configureOptions));
    }
}
