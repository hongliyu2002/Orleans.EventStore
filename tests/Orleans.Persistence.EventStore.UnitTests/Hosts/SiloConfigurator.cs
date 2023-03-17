using EventStore.Client;
using Orleans.TestingHost;

namespace Orleans.Persistence.EventStore.UnitTests.Hosts;

public class SiloConfigurator : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddEventStoreGrainStorage(Constants.TestStoreName,
                                              options =>
                                              {
                                                  var connectionString = "esdb://123.60.184.85:2113?tls=false";
                                                  options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                              });
    }
}
