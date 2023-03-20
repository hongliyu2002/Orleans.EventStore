using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Streams;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public static class EventStoreQueueStreamConfiguratorExtensions
{
    /// <summary>
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configureOptions"></param>
    public static void ConfigureEventStoreQueue(this IEventStoreQueueStreamConfigurator configurator, Action<OptionsBuilder<EventStoreQueueOptions>> configureOptions)
    {
        configurator.Configure(configureOptions);
    }

    /// <summary>
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="dataAdapterFactory"></param>
    public static void ConfigureQueueDataAdapter(this IEventStoreQueueStreamConfigurator configurator, Func<IServiceProvider, string, IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>> dataAdapterFactory)
    {
        configurator.ConfigureComponent(dataAdapterFactory);
    }

    /// <summary>
    /// </summary>
    /// <param name="configurator"></param>
    /// <typeparam name="TQueueDataAdapter"></typeparam>
    public static void ConfigureQueueDataAdapter<TQueueDataAdapter>(this IEventStoreQueueStreamConfigurator configurator)
        where TQueueDataAdapter : IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>
    {
        configurator.ConfigureComponent<IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>>((sp, n) => ActivatorUtilities.CreateInstance<TQueueDataAdapter>(sp));
    }
}
