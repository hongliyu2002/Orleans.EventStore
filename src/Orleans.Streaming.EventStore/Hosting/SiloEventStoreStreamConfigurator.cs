using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Providers.Streams.EventStore;

namespace Orleans.Hosting;

/// <summary>
///     Configures a persistent stream provider based on EventStore for use with Orleans silos.
/// </summary>
public class SiloEventStoreStreamConfigurator : SiloRecoverableStreamConfigurator, ISiloEventStoreStreamConfigurator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SiloEventStoreStreamConfigurator" /> class.
    /// </summary>
    /// <param name="name">The name of the stream provider being configured.</param>
    /// <param name="configureServicesDelegate">A delegate used to configure the services collection.</param>
    public SiloEventStoreStreamConfigurator(string name, Action<Action<IServiceCollection>> configureServicesDelegate)
        : base(name, configureServicesDelegate, EventStoreQueueAdapterFactory.Create)
    {
        ConfigureDelegate(services =>
                          {
                              services.ConfigureNamedOptionForLogging<EventStoreOptions>(name)
                                      .ConfigureNamedOptionForLogging<EventStoreReceiverOptions>(name)
                                      .ConfigureNamedOptionForLogging<EventStoreStreamCachePressureOptions>(name)
                                      .AddTransient<IConfigurationValidator>(sp => new EventStoreOptionsValidator(sp.GetOptionsByName<EventStoreOptions>(name), name))
                                      .AddTransient<IConfigurationValidator>(sp => new StreamCheckpointerConfigurationValidator(sp, name));
                          });
    }
}
