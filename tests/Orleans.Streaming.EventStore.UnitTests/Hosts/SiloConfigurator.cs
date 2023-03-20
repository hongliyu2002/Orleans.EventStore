// using EventStore.Client;
// using Orleans.TestingHost;
//
// namespace Orleans.Streaming.EventStore.UnitTests.Hosts;
//
// public class SiloConfigurator : ISiloConfigurator
// {
//     /// <inheritdoc />
//     public void Configure(ISiloBuilder siloBuilder)
//     {
//         var connectionString = "esdb://123.60.184.85:2113?tls=false";
//         siloBuilder.AddStreaming();
//         siloBuilder.AddEventStoreQueueStreams(Constants.StreamProviderName,
//                                               configurator =>
//                                               {
//                                                   configurator.ConfigureEventStoreQueue(builder =>
//                                                                                         {
//                                                                                             builder.Configure(options =>
//                                                                                                               {
//                                                                                                                   options.ClientSettings = EventStoreClientSettings.Create(connectionString);
//                                                                                                                   options.QueueNames = new List<string>
//                                                                                                                                        {
//                                                                                                                                            "Test-12345"
//                                                                                                                                        };
//                                                                                                               });
//                                                                                         });
//                                                   configurator.ConfigureCacheSize(1024);
//                                                   configurator.ConfigurePullingAgent(builder =>
//                                                                                      {
//                                                                                          builder.Configure(options =>
//                                                                                                            {
//                                                                                                                options.GetQueueMsgsTimerPeriod = TimeSpan.FromMicroseconds(1000);
//                                                                                                                options.BatchContainerBatchSize = 10;
//                                                                                                            });
//                                                                                      });
//                                               })
//                    .AddEventStoreGrainStorage(Constants.PubSubStoreName,
//                                               options =>
//                                               {
//                                                   options.ClientSettings = EventStoreClientSettings.Create(connectionString);
//                                               });
//     }
// }
