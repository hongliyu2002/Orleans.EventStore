using EventStore.Client;
using Orleans.Configuration;
using Orleans.TestingHost;
using StackExchange.Redis;
using Vending.Domain.Abstractions;

namespace Orleans.Persistence.EventStore.UnitTests.Hosts;

public class SiloConfigurator : ISiloConfigurator
{
    /// <inheritdoc />
    public void Configure(ISiloBuilder siloBuilder)
    {
        // siloBuilder.AddEventStoreGrainStorage(Constants.GrainStorageName, options =>
        //                                                                   {
        //                                                                       var connectionString = "esdb://123.60.184.85:2113?tls=false";
        //                                                                       options.ClientSettings = EventStoreClientSettings.Create(connectionString);
        //                                                                   });
        siloBuilder.AddRedisGrainStorage(Constants.GrainStorageName, options =>
                                                                   {
                                                                       options.ConfigurationOptions = ConfigurationOptions.Parse("123.60.184.85:6379,password=123456,defaultDatabase=1");
                                                                   });
    }
}