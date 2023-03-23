using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Providers.Streams.EventStore;

namespace Orleans.Hosting;

/// <summary>
///     Configuration object for a cluster client using the EventStore stream provider.
/// </summary>
public class ClusterClientEventStoreStreamConfigurator : ClusterClientPersistentStreamConfigurator, IClusterClientEventStoreStreamConfigurator
{
    /// <summary>
    ///     Constructs a new <see cref="ClusterClientEventStoreStreamConfigurator" /> object.
    /// </summary>
    /// <param name="name">The name of the stream provider to configure.</param>
    /// <param name="builder">The client builder.</param>
    public ClusterClientEventStoreStreamConfigurator(string name, IClientBuilder builder)
        : base(name, builder, EventStoreQueueAdapterFactory.Create)
    {
        builder.ConfigureServices(services =>
                                  {
                                      services.ConfigureNamedOptionForLogging<EventStoreOptions>(name)
                                              .AddTransient<IConfigurationValidator>(sp => new EventStoreOptionsValidator(sp.GetOptionsByName<EventStoreOptions>(name), name));
                                  });
    }
}
