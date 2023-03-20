using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.EventStore;
using Orleans.Streams;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public class SiloEventStoreQueueStreamConfigurator : SiloPersistentStreamConfigurator, ISiloEventStoreQueueStreamConfigurator
{
    /// <inheritdoc />
    public SiloEventStoreQueueStreamConfigurator(string name, Action<Action<IServiceCollection>> configureDelegate)
        : base(name, configureDelegate, EventStoreQueueAdapterFactory.Create)
    {
        this.ConfigureComponent(EventStoreQueueOptionsValidator.Create);
        this.ConfigureComponent(SimpleQueueCacheOptionsValidator.Create);
        //configure default queue names
        this.ConfigureEventStoreQueue(builder =>
                                      {
                                          builder.PostConfigure<IOptions<ClusterOptions>>((queueOptions, clusterOptions) =>
                                                                                          {
                                                                                              if (queueOptions.QueueNames == null || queueOptions.QueueNames.Count == 0)
                                                                                              {
                                                                                                  queueOptions.QueueNames = EventStoreQueueStreamProviderUtils.GenerateDefaultEventStoreQueueNames(clusterOptions.Value.ServiceId, Name);
                                                                                              }
                                                                                          });
                                      });
        ConfigureDelegate(services =>
                          {
                              services.TryAddSingleton<IQueueDataAdapter<ReadOnlyMemory<byte>, IBatchContainer>, EventStoreQueueDataAdapterV2>();
                          });
    }
}
