<img src="https://raw.githubusercontent.com/hongliyu2002/Orleans.FluentResult/master/resources/icons/logo_128.png" alt="Fluent Result"/>

# EventStore Provider for Microsoft Orleans Streaming

### Silo configuration:
```csharp

silo.AddStreaming();
silo.AddEventStoreStreams(Constants.StreamProviderName, configurator =>
{
    var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
    configurator.ConfigureEventStore(optionsBuilder =>
    {
        optionsBuilder.Configure(options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
            options.Name = "ChatRoomV2";
            // Configure Queues
            options.Queues = new List<string>
            {
                "ChatRoomV2-11111",
                "ChatRoomV2-22222",
                "ChatRoomV2-33333",
                "ChatRoomV2-44444"
            };
        });
    });

    configurator.ConfigureReceiver(optionsBuilder =>
    {
        optionsBuilder.Configure(options =>
        {
            options.SubscriptionSettings = new PersistentSubscriptionSettings(checkPointAfter: TimeSpan.FromMinutes(1), checkPointLowerBound: 1);
            options.PrefetchCount = 50;
        });
    });

    configurator.ConfigureCachePressuring(optionsBuilder =>
    {
        optionsBuilder.Configure(options =>
        {
            options.SlowConsumingMonitorFlowControlThreshold = 0.5;
            options.SlowConsumingMonitorPressureWindowSize = TimeSpan.FromMinutes(30);
        });
    });

    configurator.UseEventStoreCheckpointer(optionsBuilder =>
    {
        optionsBuilder.Configure(options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
            options.PersistInterval = TimeSpan.FromSeconds(30);
        });
    });

    configurator.ConfigureStreamPubSub();
    configurator.UseConsistentRingQueueBalancer();
});
```

### Cluster Client Configuration
```csharp
client.AddStreaming();
client.AddEventStoreStreams(Constants.StreamProviderName, configurator =>
{
    configurator.ConfigureEventStore(optionsBuilder =>
    {
        optionsBuilder.Configure(options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
            options.Name = "ChatRoomV2";
            options.Queues = new List<string>
            {
                "ChatRoomV2-11111",
                "ChatRoomV2-22222",
                "ChatRoomV2-33333",
                "ChatRoomV2-44444"
            };
        });
    });
    configurator.ConfigureStreamPubSub();
});
```