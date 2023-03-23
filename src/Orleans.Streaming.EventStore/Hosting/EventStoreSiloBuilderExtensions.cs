using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreSiloBuilderExtensions
{
    /// <summary>
    ///     Configure silo to use event hub persistent streams.
    /// </summary>
    public static ISiloBuilder AddEventStoreStreams(this ISiloBuilder builder, string name, Action<ISiloEventStoreStreamConfigurator> configure)
    {
        var configurator = new SiloEventStoreStreamConfigurator(name, configureServicesDelegate => builder.ConfigureServices(configureServicesDelegate));
        configure.Invoke(configurator);
        return builder;
    }

    /// <summary>
    ///     Configure silo to use event hub persistent streams with default check pointer and other settings
    /// </summary>
    public static ISiloBuilder AddEventStoreStreams(this ISiloBuilder builder, string name, Action<EventStoreOptions> configureEventStore, Action<EventStoreStreamCheckpointerOptions> configureDefaultCheckpointer)
    {
        return builder.AddEventStoreStreams(name,
                                            configurator =>
                                            {
                                                configurator.ConfigureEventStore(ob => ob.Configure(configureEventStore));
                                                configurator.UseEventStoreCheckpointer(ob => ob.Configure(configureDefaultCheckpointer));
                                            });
    }
}
