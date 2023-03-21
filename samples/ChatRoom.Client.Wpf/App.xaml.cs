using System;
using System.Windows;
using ChatRoom.Abstractions;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

namespace ChatRoom.Client.Wpf;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public IHost? AppHost { get; set; }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs args)
    {
        base.OnStartup(args);
        var hostBuilder = CreateHostBuilder(args.Args);
        AppHost = hostBuilder.Build();
        AppHost.Start();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs args)
    {
        base.OnExit(args);
        if (AppHost != null)
        {
            AppHost.WaitForShutdown();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        var redisConnectionString = "123.60.184.85:6379";
        var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
        return Host.CreateDefaultBuilder(args)
                   .UseOrleansClient(client =>
                                     {
                                         // Configure cluster.
                                         client.Configure<ClusterOptions>(options =>
                                                                          {
                                                                              options.ServiceId = "ChatService";
                                                                              options.ClusterId = "ChatCluster";
                                                                          });
                                         client.UseRedisClustering(options =>
                                                                   {
                                                                       options.ConnectionString = redisConnectionString;
                                                                       options.Database = 0;
                                                                   });
                                         // Configure streaming
                                         client.AddStreaming();
                                         client.AddEventStoreQueueStreams(Constants.StreamProviderName,
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
                                                                              configurator.ConfigureStreamPubSub();
                                                                          });
                                         client.AddBroadcastChannel(Constants.BroadcastChannelName,
                                                                    options =>
                                                                    {
                                                                        options.FireAndForgetDelivery = true;
                                                                    });
                                         // Configure transactions
                                         client.UseTransactions();
                                     });
    }
}
