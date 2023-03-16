using Microsoft.Extensions.Options;
using Orleans.EventSourcing.Configuration;
using Orleans.Providers;

namespace Orleans.EventSourcing.Hosting;

/// <summary>
/// </summary>
public static class EventStoreStorageSiloBuilderExtensions
{
    /// <summary>
    ///     Configures EventStore as the default log consistency storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreBasedLogConsistencyProviderAsDefault(this ISiloBuilder builder, Action<EventStoreStorageOptions> configureOptions)
    {
        return builder.AddEventStoreBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as a log consistency storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreBasedLogConsistencyProvider(this ISiloBuilder builder, string name, Action<EventStoreStorageOptions> configureOptions)
    {
        return builder.ConfigureServices(services => services.AddEventStoreBasedLogConsistencyProvider(name, configureOptions));
    }

    /// <summary>
    ///     Configures EventStore as the default log consistency storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreBasedLogConsistencyProviderAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        return builder.AddEventStoreBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as a log consistency storage provider.
    /// </summary>
    public static ISiloBuilder AddEventStoreBasedLogConsistencyProvider(this ISiloBuilder builder, string name, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        return builder.ConfigureServices(services => services.AddEventStoreBasedLogConsistencyProvider(name, configureOptions));
    }
}
