using System;
using System.Threading.Tasks;
using System.Windows;
using ChatRoom.Abstractions;
using ChatRoom.Abstractions.States;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace ChatRoom.Client.Wpf;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly IClusterClient _clusterClient;
    private string? _username;
    private string? _channel;
    private bool _joined;
    private IChannelGrain? _room;
    private StreamId _streamId;
    private IStreamProvider? _streamProvider;
    private IAsyncStream<ChatMessage>? _stream;
    private Task<StreamSubscriptionHandle<ChatMessage>>? _subscription;

    public MainWindow(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        InitializeComponent();
    }

    private async void JoinLeaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_joined)
        {
            var inputWindow = new InputWindow();
            var result = inputWindow.ShowDialog();
            if (result == true)
            {
                _username = inputWindow.Username ?? "???";
                _channel = inputWindow.Channel ?? "???";
                _room = _clusterClient.GetGrain<IChannelGrain>(_channel);
                _streamId = await _room.Join(_username);
                _streamProvider = _clusterClient.GetStreamProvider(Constants.StreamProviderName);
                _stream = _streamProvider.GetStream<ChatMessage>(_streamId);
                _subscription = _stream.SubscribeAsync((message, token) =>
                                                       {
                                                           MessagesListBox.Dispatcher.Invoke(() =>
                                                                                             {
                                                                                                 MessagesListBox.Items.Add($"#{token.SequenceNumber}.{token.EventIndex} {message}");
                                                                                             });
                                                           return Task.CompletedTask;
                                                       });
                UsernameText.Text = _username;
                ChannelText.Text = _channel;
                JoinLeaveButton.Content = "Leave";
                _joined = true;
            }
        }
        else
        {
            _joined = false;
            JoinLeaveButton.Content = "Join";
            ChannelText.Text = String.Empty;
            UsernameText.Text = String.Empty;
            _subscription?.Dispose();
            _stream = null;
            _streamProvider = null;
            await _room?.Leave(_username);
            _room = null;
            _channel = null;
            _username = null;
        }
    }

    private void ReadHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void ShowMembersButton_OnClick(object sender, RoutedEventArgs e)
    {
    }

    private void SendMessageButton_OnClick(object sender, RoutedEventArgs e)
    {
    }
}
