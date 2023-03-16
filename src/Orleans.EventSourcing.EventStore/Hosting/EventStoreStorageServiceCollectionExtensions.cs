using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.EventSourcing.EventStore.Configuration;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.EventSourcing.EventStore.Hosting;

/// <summary>
/// </summary>
public static class EventStoreStorageServiceCollectionExtensions
{
    /// <summary>
    ///     Configures EventStore as the default log consistency storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreBasedLogConsistencyProviderAsDefault(this IServiceCollection services, Action<EventStoreStorageOptions> configureOptions)
    {
        return services.AddEventStoreBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    ///     Configures EventStore as a log consistency storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreBasedLogConsistencyProvider(this IServiceCollection services, string name, Action<EventStoreStorageOptions> configureOptions)
    {
        return services.AddEventStoreBasedLogConsistencyProvider(name, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    ///     Configures EventStore as the default log consistency storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreBasedLogConsistencyProviderAsDefault(this IServiceCollection services, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        return services.AddEventStoreBasedLogConsistencyProvider(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as a log consistency storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreBasedLogConsistencyProvider(this IServiceCollection services, string name, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        // Configure storage.
        configureOptions?.Invoke(services.AddOptions<EventStoreStorageOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp => new EventStoreStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>().Get(name), name));
        services.AddTransient<IPostConfigureOptions<EventStoreStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStoreStorageOptions>>();
        services.ConfigureNamedOptionForLogging<EventStoreStorageOptions>(name);
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp => sp.GetServiceByName<ILogConsistentStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }
        services.AddSingletonNamedService(name, EventStoreLogConsistentStorageFactory.Create);
        services.AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (sp, n) => (ILifecycleParticipant<ISiloLifecycle>)sp.GetRequiredServiceByName<ILogConsistentStorage>(n));

        // Configure log view adaptor.
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp => sp.GetServiceByName<ILogViewAdaptorFactory>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }
        return services.AddSingletonNamedService<ILogViewAdaptorFactory, LogConsistencyProvider>(name);
    }
}
