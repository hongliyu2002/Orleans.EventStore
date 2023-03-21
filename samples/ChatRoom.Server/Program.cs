using System.Net;
using System.Runtime.InteropServices;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Serilog;
using Serilog.Events;

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
        var redisConnectionString = "123.60.184.85:6379";
        var eventStoreConnectionString = "esdb://123.60.184.85:2113";
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
                                                                       options.AdvertisedIPAddress = IPAddress.Loopback;
                                                                       options.SiloPort = 11111;
                                                                       options.GatewayPort = 30000;
                                                                   });
                                   silo.UseRedisClustering(options =>
                                                           {
                                                               options.ConnectionString = redisConnectionString;
                                                               options.Database = 0;
                                                           });
                                   // Configure reminder service.
                                   silo.AddReminders();
                                   silo.UseRedisReminderService(options =>
                                                                {
                                                                    options.ConnectionString = redisConnectionString;
                                                                    options.DatabaseNumber = 0;
                                                                });
                                   // Configure grain storage
                                   silo.AddRedisGrainStorage(Constants.PubSubStoreName,
                                                             options =>
                                                             {
                                                                 options.ConnectionString = redisConnectionString;
                                                                 options.DatabaseNumber = 1;
                                                                 options.DeleteOnClear = true;
                                                             });
                                   // Configure streaming
                                   silo.AddStreaming();
                                   silo.AddEventStoreQueueStreams(Constants.StreamProviderName,
                                                                  configurator =>
                                                                  {
                                                                      configurator.ConfigureEventStoreQueue(optionsBuilder =>
                                                                                                            {
                                                                                                                optionsBuilder.Configure(options =>
                                                                                                                                         {
                                                                                                                                             options.ClientSettings = EventStoreClientSettings.Create(eventStoreConnectionString);
                                                                                                                                             options.SubscriptionSettings = new PersistentSubscriptionSettings(checkPointAfter: TimeSpan.FromSeconds(30));
                                                                                                                                             options.TotalQueueCount = 4;
                                                                                                                                             options.QueueBufferSize = 10;
                                                                                                                                         });
                                                                                                            });
                                                                      configurator.ConfigurePullingAgent(optionsBuilder =>
                                                                                                         {
                                                                                                             optionsBuilder.Configure(options =>
                                                                                                                                      {
                                                                                                                                          options.BatchContainerBatchSize = 10;
                                                                                                                                          options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(200);
                                                                                                                                      });
                                                                                                         });
                                                                      configurator.ConfigureCacheSize(1024);
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
