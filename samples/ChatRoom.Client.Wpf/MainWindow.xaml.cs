using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChatRoom.Abstractions;
using ChatRoom.Abstractions.States;
using Fluxera.Utilities.Extensions;
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

    private string? _currentUsername;
    private string? _currentChannel;

    private IStreamProvider _streamProvider = null!;
    private StreamId _streamId;
    private IAsyncStream<ChatMessage>? _stream;
    private StreamSubscriptionHandle<ChatMessage>? _subscription;

    private IChannelGrain? _channelGrain;

    private bool _joined;

    public MainWindow(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
        InitializeComponent();
    }

    #region Window Lifecycle

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        _streamProvider = _clusterClient.GetStreamProvider(Constants.StreamProviderName);
        RefreshControls();
    }

    private async void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        await LeaveChannel();
    }

    #endregion

    private void RefreshControls()
    {
        if (_joined)
        {
            UsernameText.Text = _currentUsername;
            ChannelText.Text = _currentChannel;
            JoinLeaveButton.Content = "Leave";
            ReadHistoryButton.IsEnabled = true;
            ShowMembersButton.IsEnabled = true;
            SendMessageButton.IsEnabled = true;
        }
        else
        {
            UsernameText.Text = string.Empty;
            ChannelText.Text = string.Empty;
            JoinLeaveButton.Content = "Join";
            ReadHistoryButton.IsEnabled = false;
            ShowMembersButton.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
        }
        MessagesListBox.Items.Clear();
    }

    private async void JoinLeaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_joined)
        {
            await LeaveChannel();
        }
        else
        {
            var inputWindow = new InputWindow();
            var result = inputWindow.ShowDialog();
            if (result is true)
            {
                await JoinChannel(inputWindow.Username, inputWindow.Channel);
            }
        }
        RefreshControls();
    }

    private async void ReadHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshControls();
        var messages = await ReadHistory(100);
        messages.ForEach(message => MessagesListBox.Items.Add(message.ToString()));
    }

    private async void ShowMembersButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshControls();
        var members = await GetMembers();
        members.ForEach(member => MessagesListBox.Items.Add(member));
    }

    private async void SendMessageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var text = MessageText.Text;
        if (text.IsNullOrEmpty())
        {
            MessageBox.Show(this, "Message text should be input...", "Error occurred, Please try again...");
        }
        else
        {
            await SendMessage(text);
            MessageText.Text = string.Empty;
        }
    }

    private async Task JoinChannel(string? username, string? channel)
    {
        try
        {
            _currentUsername = username.IsNullOrWhiteSpace() ? "(anonymous)" : username;
            _currentChannel = channel.IsNullOrWhiteSpace() ? "(channel unknown)" : channel;
            _channelGrain = _clusterClient.GetGrain<IChannelGrain>(_currentChannel);
            _streamId = await _channelGrain.Join(_currentUsername!);
            _stream = _streamProvider.GetStream<ChatMessage>(_streamId);
            // var subscriptions = await _stream.GetAllSubscriptionHandles();
            // if (subscriptions is { Count: > 0 })
            // {
            //     await Task.WhenAll(subscriptions.Select(subscription => subscription.ResumeAsync(new StreamObserver(MessagesListBox))));
            // }
            _subscription = await _stream.SubscribeAsync(new StreamObserver(MessagesListBox));
            _joined = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error occurred, Please try again...");
        }
    }

    private async Task LeaveChannel()
    {
        try
        {
            if (_channelGrain != null && _currentUsername.IsNotNullOrWhiteSpace())
            {
                await _channelGrain.Leave(_currentUsername!);
                _channelGrain = null;
            }
            if (_subscription != null)
            {
                await _subscription.UnsubscribeAsync();
                _subscription = null;
            }
            if (_stream != null)
            {
                var subscriptions = await _stream.GetAllSubscriptionHandles();
                if (subscriptions is { Count: > 0 })
                {
                    await Task.WhenAll(subscriptions.Select(subscription => subscription.UnsubscribeAsync()));
                }
                _stream = null;
            }
            _currentUsername = null;
            _currentChannel = null;
            _joined = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error occurred, Please try again...");
        }
    }

    private async Task<ChatMessage[]> ReadHistory(int maxCount)
    {
        if (!_joined)
        {
            return Array.Empty<ChatMessage>();
        }
        try
        {
            if (_channelGrain != null)
            {
                return await _channelGrain.ReadHistory(maxCount);
            }
            return Array.Empty<ChatMessage>();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error occurred, Please try again...");
            return Array.Empty<ChatMessage>();
        }
    }

    private async Task<string[]> GetMembers()
    {
        if (!_joined)
        {
            return Array.Empty<string>();
        }
        try
        {
            if (_channelGrain != null)
            {
                return await _channelGrain.GetMembers();
            }
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error occurred, Please try again...");
            return Array.Empty<string>();
        }
    }

    private async Task SendMessage(string text)
    {
        if (!_joined)
        {
            return;
        }
        try
        {
            if (_channelGrain != null && _currentUsername.IsNotNullOrWhiteSpace())
            {
                var messageText = text.IsNullOrWhiteSpace() ? string.Empty : text;
                await _channelGrain.SendMessage(new ChatMessage(_currentUsername!, messageText, DateTimeOffset.Now));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error occurred, Please try again...");
        }
    }

    private class StreamObserver : IAsyncObserver<ChatMessage>
    {
        private readonly ListBox _listBox;

        public StreamObserver(ListBox listBox)
        {
            _listBox = listBox;
        }

        /// <inheritdoc />
        public Task OnNextAsync(ChatMessage message, StreamSequenceToken? token = null)
        {
            var messageString = token == null ? message.ToString() : $"#{token.SequenceNumber}.{token.EventIndex} {message}";
            _listBox.Dispatcher.Invoke(() => _listBox.Items.Add(messageString));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnErrorAsync(Exception ex)
        {
            _listBox.Items.Add($"Error occurred: {ex.Message}");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }
    }

}
