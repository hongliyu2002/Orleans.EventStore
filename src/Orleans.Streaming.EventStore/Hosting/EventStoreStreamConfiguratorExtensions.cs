using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.EventStore;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreStreamConfiguratorExtensions
{

    /// <summary>
    ///     Extension method to configure the EventStore stream provider options.
    /// </summary>
    /// <param name="configurator">The EventStore stream configurator.</param>
    /// <param name="configureOptions">The action to configure the options builder.</param>
    public static void ConfigureEventStore(this IEventStoreStreamConfigurator configurator, Action<OptionsBuilder<EventStoreOptions>> configureOptions)
    {
        configurator.Configure(configureOptions);
    }

    /// <summary>
    ///     Extension method to configure the data adapter used by the EventStore stream provider.
    /// </summary>
    /// <param name="configurator">The EventStore stream configurator.</param>
    /// <param name="dataAdapterFactory">The factory method to create the data adapter.</param>
    public static void ConfigureDataAdapter(this IEventStoreStreamConfigurator configurator, Func<IServiceProvider, string, IEventStoreDataAdapter> dataAdapterFactory)
    {
        configurator.ConfigureComponent(dataAdapterFactory);
    }

}
