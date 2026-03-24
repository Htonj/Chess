using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;
using System.Timers;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Linq;

namespace ChessApp;

public partial class MainWindow : Window
{
    
    public MainWindow()
    {
        InitializeComponent();
        
    }

    private void BotGameButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GameWindow gw = new GameWindow();
        gw.Show();
        this.Close();
    }
    
    private void LocalGameButton_OnClick(object? sender, RoutedEventArgs e)
    {
        LocalGameWindow lgw = new LocalGameWindow();
        lgw.Show();
        this.Close();
    }
}