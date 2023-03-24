using System;
using System.Collections.Generic;
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
        // var redisConnectionString = "123.60.184.85:6379";
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
                                         client.UseLocalhostClustering();
                                         // client.UseRedisClustering(options =>
                                         //                           {
                                         //                               options.ConnectionString = redisConnectionString;
                                         //                               options.Database = 0;
                                         //                           });
                                         // Configure streaming
                                         client.AddStreaming();
                                         client.AddEventStoreStreams(Constants.StreamProviderName,
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
