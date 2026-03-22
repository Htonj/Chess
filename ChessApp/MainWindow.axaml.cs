using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ChessApp.Model;
using System.IO;
using System.Timers;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Threading;

namespace ChessApp;

public partial class MainWindow : Window
{
    DispatcherTimer ptimer = new DispatcherTimer();
    public int ptscounter = 1;
    public int ptmcounter = 10;
    DispatcherTimer btimer = new DispatcherTimer();
    public int btscounter = 1;
    public int btmcounter = 10;
    public MainWindow()
    {
        InitializeComponent();
        char[] chrow = new char[8] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Cell cell = new Cell();
                
                string cellName = chrow[j] + (8 - i).ToString();
                
                if ((i + j) % 2 == 0)
                    cell.Background = Brush.Parse("#323232");
                else
                    cell.Background = Brush.Parse("#282828");
                
                FillCells(cell, cellName);
                
                CellsList.Cells.Add(cell);
                CellsWrap.Children.Add(cell);
            }
        }
        
        ptimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        ptimer.Tick += new EventHandler(PlayerTimerOut);
        ptimer.Start();
        
        btimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        btimer.Tick += new EventHandler(PlayerTimerOut);
    }

    private void PlayerTimerOut(object sender, EventArgs e)
    {
        ptscounter--;
        if (ptscounter == 0)
        {
            ptmcounter--;
            ptscounter = 59;
            if (ptmcounter == 0)
            {
                ptimer.Stop();
            }
        }
        PlayerTimerLabel.Content = ptmcounter.ToString() + ":" + ptscounter.ToString();
    }
    private void BotTimerOut(object sender, EventArgs e)
    {
        btscounter--;
        if (btscounter == 0)
        {
            btmcounter--;
            btscounter = 59;
            if (btmcounter == 0)
            {
                btimer.Stop();
            }
        }
        BotTimerLabel.Content = btmcounter.ToString() + ":" + btscounter.ToString();
    }
    
    private void FillCells(Cell cell, string cellName)
    {
        if (cellName == "a1" || cellName == "h1")
            cell.Content = " ♜";
        else if (cellName == "b1" || cellName == "g1")
            cell.Content = " ♞";
        else if (cellName == "c1" || cellName == "f1")
            cell.Content = " ♝";
        else if (cellName == "d1")
            cell.Content = " ♛";
        else if (cellName == "e1")
            cell.Content = " ♚";
        else if (cellName[1] == '2')
            cell.Content = " ♟";
        
        else if (cellName == "a8" || cellName == "h8")
            cell.Content = " ♖";
        else if (cellName == "b8" || cellName == "g8")
            cell.Content = " ♘";
        else if (cellName == "c8" || cellName == "f8")
            cell.Content = " ♗";
        else if (cellName == "d8")
            cell.Content = " ♕";
        else if (cellName == "e8")
            cell.Content = " ♔";
        else if (cellName[1] == '7')
            cell.Content = " ♙";
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
