using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.EventStore;
using Orleans.Streams;

namespace Orleans.Hosting;

/// <summary>
/// </summary>
public class ClusterClientEventStoreQueueStreamConfigurator : ClusterClientPersistentStreamConfigurator, IClusterClientEventStoreQueueStreamConfigurator
{
    /// <inheritdoc />
    public ClusterClientEventStoreQueueStreamConfigurator(string name, IClientBuilder clientBuilder)
        : base(name, clientBuilder, EventStoreQueueAdapterFactory.Create)
    {
        this.ConfigureComponent(EventStoreQueueOptionsValidator.Create);
        //configure default queue names
        this.ConfigureEventStoreQueue(optionsBuilder =>
                                      {
                                          optionsBuilder.PostConfigure<IOptions<ClusterOptions>>((queueOptions, clusterOptions) =>
                                                                                                 {
                                                                                                     if (queueOptions.QueueNames == null || queueOptions.QueueNames?.Count == 0)
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
