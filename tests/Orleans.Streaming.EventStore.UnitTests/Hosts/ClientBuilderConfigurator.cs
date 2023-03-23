using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace Orleans.Streaming.EventStore.UnitTests.Hosts;

public class ClientBuilderConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        var connectionString = "esdb://123.60.184.85:2113?tls=false";
        clientBuilder.AddEventStoreStreams(Constants.StreamProviderName,
                                           options =>
                                           {
                                               options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                               options.Name = "NyApp";
                                               options.Queues = new List<string>
                                                                {
                                                                    // "test-v2-98765",
                                                                    "test-v2-43210"
                                                                };
                                           });
    }
}
