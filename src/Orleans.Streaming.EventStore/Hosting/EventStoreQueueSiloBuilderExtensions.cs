using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreQueueSiloBuilderExtensions
{
    /// <summary>
    ///     Configure silo to use EventStore queue persistent streams.
    /// </summary>
    public static ISiloBuilder AddEventStoreQueueStreams(this ISiloBuilder builder, string name, Action<SiloEventStoreQueueStreamConfigurator> configure)
    {
        var configurator = new SiloEventStoreQueueStreamConfigurator(name,
                                                                     configureServicesDelegate =>
                                                                     {
                                                                         builder.ConfigureServices(configureServicesDelegate);
                                                                     });
        configure.Invoke(configurator);
        return builder;
    }

    /// <summary>
    ///     Configure silo to use EventStore queue persistent streams with default settings
    /// </summary>
    public static ISiloBuilder AddEventStoreQueueStreams(this ISiloBuilder builder, string name, Action<OptionsBuilder<EventStoreQueueOptions>> configureOptions)
    {
        builder.AddEventStoreQueueStreams(name,
                                          configurator =>
                                          {
                                              configurator.ConfigureEventStoreQueue(configureOptions);
                                          });
        return builder;
    }
}
