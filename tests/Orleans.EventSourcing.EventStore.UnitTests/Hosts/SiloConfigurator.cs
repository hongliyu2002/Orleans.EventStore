using EventStore.Client;
using Orleans.TestingHost;

namespace Orleans.EventSourcing.EventStore.UnitTests.Hosts;

public class SiloConfigurator : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddEventStoreBasedLogConsistencyProvider(Constants.LogConsistencyStoreName,
                                                             options =>
                                                             {
                                                                 var connectionString = "esdb://123.60.184.85:2113?tls=false";
                                                                 options.ClientSettings = EventStoreClientSettings.Create(connectionString);
                                                             })
                   .AddMemoryGrainStorage(Constants.LogSnapshotStoreName);
    }
}
