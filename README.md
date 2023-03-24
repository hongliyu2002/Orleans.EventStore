# EventStore Providers for Microsoft Orleans

## Event Sourcing
### Silo Configuration
```csharp
var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
silo.AddEventStoreBasedLogConsistencyProvider(Constants.LogConsistencyStoreName, 
        options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
        })
.AddMemoryGrainStorage(Constants.LogSnapshotStoreName);
```

## Persistence

### Silo Configuration
```csharp
var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
silo.AddEventStoreGrainStorage(Constants.StateStoreName, 
        options =>
        {
            options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
        })
```
*Using EventStore DB as a Grain Storage Provider has an interesting feature: all data of the grain state changes are kept in EventStore, just like some time-series databases. By subscribing to the stream of this state, the data can be dynamically updated to the state database, such as SQL Server, making it easy to implement certain CQRS functionality.*

## Streaming
*Now supports rewindable feature!*
```csharp
private async Task JoinChannel(string? username, string? channel, long version)
{
    try
    {
        _currentUsername = username.IsNullOrWhiteSpace() ? "(anonymous)" : username;
        _currentChannel = channel.IsNullOrWhiteSpace() ? "(channel unknown)" : channel;
        _channelGrain = _clusterClient.GetGrain<IChannelGrain>(_currentChannel);
        _streamId = await _channelGrain.Join(_currentUsername!);
        _stream = _streamProvider.GetStream<ChatMessage>(_streamId);
        // Providing a specific SequenceToken allows subscribing from a specific point in time.
        _subscription = await _stream.SubscribeAsync(new StreamObserver(MessagesListBox), new EventSequenceTokenV2(version));
        _joined = true;
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, ex.Message, "Error occurred, Please try again...");
    }
}
```

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

### Cluster Client Configuration:
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