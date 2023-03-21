using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;

namespace Orleans.Streaming.EventStore.UnitTests.Hosts;

public class SiloConfigurator : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(ISiloBuilder siloBuilder)
    {
        var connectionString = "esdb://123.60.184.85:2113?tls=false";
        siloBuilder.Services.AddLogging(builder => builder.AddProvider(new TestOutputLoggerProvider()));
        siloBuilder.AddStreaming();
        siloBuilder.AddEventStoreQueueStreams(Constants.StreamProviderName,
                                              configurator =>
                                              {
                                                  configurator.ConfigureEventStoreQueue(builder =>
                                                                                        {
                                                                                            builder.Configure(options =>
                                                                                                              {
                                                                                                                  options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                                                                                                  options.TotalQueueCount = 4;
                                                                                                              });
                                                                                        });
                                                  configurator.ConfigureCacheSize(1024);
                                                  configurator.ConfigurePullingAgent(builder =>
                                                                                     {
                                                                                         builder.Configure(options =>
                                                                                                           {
                                                                                                               options.GetQueueMsgsTimerPeriod = TimeSpan.FromMicroseconds(200);
                                                                                                               options.BatchContainerBatchSize = 10;
                                                                                                           });
                                                                                     });
                                              })
                   .AddEventStoreGrainStorage(Constants.PubSubStoreName,
                                              options =>
                                              {
                                                  options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                              });
    }
}
