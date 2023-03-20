using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreQueueClientBuilderExtensions
{
    /// <summary>
    ///     Configure cluster client to use EventStore queue persistent streams.
    /// </summary>
    public static IClientBuilder AddEventStoreQueueStreams(this IClientBuilder builder, string name, Action<ClusterClientEventStoreQueueStreamConfigurator> configure)
    {
        //the constructor wires up DI with EventStoreQueueStream, so has to be called regardless configure is null or not
        var configurator = new ClusterClientEventStoreQueueStreamConfigurator(name, builder);
        configure.Invoke(configurator);
        return builder;
    }

    /// <summary>
    ///     Configure cluster client to use EventStore queue persistent streams.
    /// </summary>
    public static IClientBuilder AddEventStoreQueueStreams(this IClientBuilder builder, string name, Action<OptionsBuilder<EventStoreQueueOptions>> configureOptions)
    {
        builder.AddEventStoreQueueStreams(name,
                                          configurator =>
                                          {
                                              configurator.ConfigureEventStoreQueue(configureOptions);
                                          });
        return builder;
    }
}
