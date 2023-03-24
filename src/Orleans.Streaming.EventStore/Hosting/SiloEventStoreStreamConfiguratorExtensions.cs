using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.EventStore;
using Orleans.Streams;

namespace Orleans.Hosting;

/// <summary>
///     This class contains extension methods for configuring EventStore stream settings in a Silo environment.
/// </summary>
public static class SiloEventStoreStreamConfiguratorExtensions
{
    /// <summary>
    ///     Configures a checkpointer for the EventStore stream.
    /// </summary>
    /// <typeparam name="TOptions">The options type for the checkpointer.</typeparam>
    /// <param name="configurator">The stream configurator.</param>
    /// <param name="checkpointerFactoryBuilder">A delegate that creates the checkpointer factory.</param>
    /// <param name="configureOptions">An action to configure the options for the checkpointer.</param>
    public static void ConfigureCheckpointer<TOptions>(this ISiloEventStoreStreamConfigurator configurator, Func<IServiceProvider, string, IStreamQueueCheckpointerFactory> checkpointerFactoryBuilder, Action<OptionsBuilder<TOptions>> configureOptions)
        where TOptions : class, new()
    {
        configurator.ConfigureComponent(checkpointerFactoryBuilder, configureOptions);
    }

    /// <summary>
    ///     Configures the EventStore stream receiver options.
    /// </summary>
    /// <param name="configurator">The stream configurator.</param>
    /// <param name="configureOptions">An action to configure the options for the receiver.</param>
    public static void ConfigureReceiver(this ISiloEventStoreStreamConfigurator configurator, Action<OptionsBuilder<EventStoreReceiverOptions>> configureOptions)
    {
        configurator.Configure(configureOptions);
    }

    /// <summary>
    ///     Configures the cache pressure options for the EventStore stream.
    /// </summary>
    /// <param name="configurator">The stream configurator.</param>
    /// <param name="configureOptions">An action to configure the options for the cache pressure.</param>
    public static void ConfigureCachePressuring(this ISiloEventStoreStreamConfigurator configurator, Action<OptionsBuilder<EventStoreStreamCachePressureOptions>> configureOptions)
    {
        configurator.Configure(configureOptions);
    }

    /// <summary>
    ///     Configures EventStore as backend storage of checkpointer state.
    /// </summary>
    /// <param name="configurator">The stream configurator.</param>
    /// <param name="configureOptions">An action to configure the options for the checkpointer.</param>
    public static void UseEventStoreCheckpointer(this ISiloEventStoreStreamConfigurator configurator, Action<OptionsBuilder<EventStoreStreamCheckpointerOptions>> configureOptions)
    {
        configurator.ConfigureCheckpointer(EventStoreCheckpointerFactory.CreateFactory, configureOptions);
    }
}
