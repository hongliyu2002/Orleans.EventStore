using System;
using System.Windows;
using ChatRoom.Abstractions;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;

namespace ChatRoom.Client.Wpf;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly IHost _host;

    /// <inheritdoc />
    public App()
    {
        _host = CreateHostBuilder().Build();
    }

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs args)
    {
        base.OnStartup(args);
        await _host.StartAsync();
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs args)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }
        base.OnExit(args);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        var redisConnectionString = "123.60.184.85:6379";
        var eventStoreConnectionString = "esdb://123.60.184.85:2113?tls=false";
        return Host.CreateDefaultBuilder()
                   .ConfigureServices(services =>
                                      {
                                          services.AddSingleton<MainWindow>();
                                      })
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
