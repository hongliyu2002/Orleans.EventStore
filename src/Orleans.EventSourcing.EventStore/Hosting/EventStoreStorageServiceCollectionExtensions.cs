using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.EventSourcing.Configuration;
using Orleans.EventSourcing.EventStoreStorage;
using Orleans.EventSourcing.LogConsistency;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.EventSourcing.Hosting;

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
        // Configure log storage.
        configureOptions?.Invoke(services.AddOptions<EventStoreStorageOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp => new EventStoreStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>().Get(name), name));
        services.AddTransient<IPostConfigureOptions<EventStoreStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStoreStorageOptions>>();
        services.ConfigureNamedOptionForLogging<EventStoreStorageOptions>(name);
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp => sp.GetServiceByName<ILogConsistentStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }
        services.AddSingletonNamedService<ILogConsistentStorage>(name, EventStoreLogConsistentStorageFactory.Create);
        services.AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (sp, n) => (ILifecycleParticipant<ISiloLifecycle>)sp.GetRequiredServiceByName<ILogConsistentStorage>(n));

        // Configure log consistency.
        services.TryAddSingleton<Factory<IGrainContext, ILogConsistencyProtocolServices>>(serviceProvider =>
                                                                                          {
                                                                                              var protocolServicesFactory = ActivatorUtilities.CreateFactory(typeof(DefaultProtocolServices), new[] { typeof(IGrainContext) });
                                                                                              return grainContext => (ILogConsistencyProtocolServices)protocolServicesFactory(serviceProvider, new object[] { grainContext });
                                                                                          });

        // Configure log view adaptor.
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp => sp.GetServiceByName<ILogViewAdaptorFactory>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }
        return services.AddSingletonNamedService<ILogViewAdaptorFactory>(name, LogConsistencyProviderFactory.Create);
    }
}
