using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreStorageServiceCollectionExtensions
{
    /// <summary>
    ///     Configures EventStore as the default grain storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreGrainStorageAsDefault(this IServiceCollection services, Action<EventStoreStorageOptions> configureOptions)
    {
        return services.AddEventStoreGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    ///     Configures EventStore as a grain storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreGrainStorage(this IServiceCollection services, string name, Action<EventStoreStorageOptions> configureOptions)
    {
        return services.AddEventStoreGrainStorage(name, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    ///     Configures EventStore as the default grain storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreGrainStorageAsDefault(this IServiceCollection services, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        return services.AddEventStoreGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as a grain storage provider.
    /// </summary>
    public static IServiceCollection AddEventStoreGrainStorage(this IServiceCollection services, string name, Action<OptionsBuilder<EventStoreStorageOptions>>? configureOptions = null)
    {
        configureOptions?.Invoke(services.AddOptions<EventStoreStorageOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp => new EventStoreStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<EventStoreStorageOptions>>().Get(name), name));
        services.AddTransient<IPostConfigureOptions<EventStoreStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<EventStoreStorageOptions>>();
        services.ConfigureNamedOptionForLogging<EventStoreStorageOptions>(name);
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton(sp => sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
        }
        services.AddSingletonNamedService<IGrainStorage>(name, EventStoreGrainStorageFactory.Create);
        services.AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (sp, n) => (ILifecycleParticipant<ISiloLifecycle>)sp.GetRequiredServiceByName<IGrainStorage>(n));
        return services;
    }
}
