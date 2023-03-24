using System.Runtime.InteropServices;
using ChatRoom.Abstractions;
using EventStore.Client;
using Orleans.Configuration;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

namespace ChatRoom.Server;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Override("Microsoft", LogEventLevel.Information).Enrich.FromLogContext().WriteTo.Console().CreateBootstrapLogger();
        var appName = "Chat Room";
        try
        {
            Log.Information("Starting {Application}...", appName);
            var hostBuilder = CreateHostBuilder(args);
            var host = hostBuilder.Build();
            Log.Information("Started {Application} with runtime {Runtime} and OS {OperatingSystem}.", appName, RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription);
            await host.RunAsync();
            Log.Information("Stopped {Application} with runtime {Runtime} and OS {OperatingSystem}.", appName, RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "{Application} terminated unexpectedly with runtime {Runtime} and OS {OperatingSystem}.", appName, RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription);
            return -1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        // var redisConnectionString0 = "123.60.184.85:6379,defaultDatabase=0";
        var redisConnectionString1 = "123.60.184.85:6379,defaultDatabase=1";
        var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
        return Host.CreateDefaultBuilder(args)
                   .UseOrleans(silo =>
                               {
                                   // Configure cluster.
                                   silo.Configure<ClusterOptions>(options =>
                                                                  {
                                                                      options.ServiceId = "ChatService";
                                                                      options.ClusterId = "ChatCluster";
                                                                  });
                                   silo.Configure<EndpointOptions>(options =>
                                                                   {
                                                                       options.SiloPort = 11111;
                                                                       options.GatewayPort = 30000;
                                                                   });
                                   // silo.UseRedisClustering(options =>
                                   //                         {
                                   //                             options.ConnectionString = redisConnectionString;
                                   //                             options.Database = 0;
                                   //                         });
                                   silo.UseLocalhostClustering();
                                   // Configure reminder service.
                                   silo.AddReminders();
                                   // silo.UseRedisReminderService(options =>
                                   //                              {
                                   //                                  options.ConnectionString = redisConnectionString;
                                   //                                  options.DatabaseNumber = 0;
                                   //                              });
                                   silo.UseInMemoryReminderService();

                                   // Configure grain storage
                                   silo.AddRedisGrainStorage(Constants.PubSubStoreName,
                                                             options =>
                                                             {
                                                                 options.ConfigurationOptions = ConfigurationOptions.Parse(redisConnectionString1);
                                                             });
                                   // Configure streaming
                                   silo.AddStreaming();
                                   silo.AddEventStoreStreams(Constants.StreamProviderName,
                                                             configurator =>
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
                                   silo.AddBroadcastChannel(Constants.BroadcastChannelName,
                                                            options =>
                                                            {
                                                                options.FireAndForgetDelivery = true;
                                                            });
                                   // Configure transaction
                                   silo.UseTransactions();
                               })
                   .UseSerilog((context, serviceProvider, logConfig) =>
                               {
                                   logConfig.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(serviceProvider);
                               })
                   .UseConsoleLifetime();
    }
}
