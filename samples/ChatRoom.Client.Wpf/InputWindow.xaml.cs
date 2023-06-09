﻿using System.Windows;

namespace ChatRoom.Client.Wpf;

/// <summary>
///     InputWindow.xaml 的交互逻辑
/// </summary>
public partial class InputWindow : Window
{
    public string? Username { get; set; }

    public string? Channel { get; set; }
    
    public long Version { get; set; }

    public InputWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Username = UsernameTextBox.Text;
        Channel = ChannelTextBox.Text;
        if (long.TryParse(VersionTextBox.Text, out var version))
        {
            Version = version;
        }
        DialogResult = true;
    }
}
