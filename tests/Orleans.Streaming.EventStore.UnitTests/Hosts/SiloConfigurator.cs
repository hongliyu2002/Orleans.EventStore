using EventStore.Client;
using Orleans.TestingHost;

namespace Orleans.Streaming.EventStore.UnitTests.Hosts;

public class SiloConfigurator : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(ISiloBuilder siloBuilder)
    {
        var connectionString = "esdb://123.60.184.85:2113?tls=false";
        siloBuilder.AddStreaming();
        siloBuilder.AddEventStoreStreams(Constants.StreamProviderName,
                                         options =>
                                         {
                                             options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                             options.Name = "NyApp";
                                             options.Queues = new List<string>
                                                              {
                                                                  // "test-v2-98765",
                                                                  "test-v2-43210"
                                                              };
                                         },
                                         checkpointOptions =>
                                         {
                                             checkpointOptions.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                             checkpointOptions.PersistInterval = TimeSpan.FromSeconds(10);
                                         })
                   .AddEventStoreGrainStorage(Constants.PubSubStoreName,
                                              options =>
                                              {
                                                  options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                              });
    }
}
