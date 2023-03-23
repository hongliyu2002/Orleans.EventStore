using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreClientBuilderExtensions
{
    /// <summary>
    ///     Configure cluster client to use event hub persistent streams.
    /// </summary>
    public static IClientBuilder AddEventStoreStreams(this IClientBuilder builder, string name, Action<IClusterClientEventStoreStreamConfigurator> configure)
    {
        var configurator = new ClusterClientEventStoreStreamConfigurator(name, builder);
        configure.Invoke(configurator);
        return builder;
    }

    /// <summary>
    ///     Configure cluster client to use event hub persistent streams with default settings.
    /// </summary>
    public static IClientBuilder AddEventStoreStreams(this IClientBuilder builder, string name, Action<EventStoreOptions> configureEventStore)
    {
        builder.AddEventStoreStreams(name,
                                     configurator =>
                                     {
                                         configurator.ConfigureEventStore(ob => ob.Configure(configureEventStore));
                                     });
        return builder;
    }
}
