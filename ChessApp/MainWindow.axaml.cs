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
    DispatcherTimer ptimer = new DispatcherTimer();
    public int ptscounter = 1;
    public int ptmcounter = 10;
    DispatcherTimer btimer = new DispatcherTimer();
    public int btscounter = 1;
    public int btmcounter = 10;
    
    private Button? selectedCell = null;
    private bool isPlayerTurn = true;
    private Dictionary<string, Button> boardCells = new Dictionary<string, Button>();
    private string? enPassantTarget = null;
    private Dictionary<string, string> originalContent = new Dictionary<string, string>(); // Для сохранения оригинального контента
    
    public MainWindow()
    {
        InitializeComponent();
        InitializeBoard();
        
        ptimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        ptimer.Tick += new EventHandler(PlayerTimerOut);
        ptimer.Start();
        
        btimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        btimer.Tick += new EventHandler(BotTimerOut);
        
        UpdateTimerDisplay();
    }

    private void InitializeBoard()
    {
        char[] chrow = new char[8] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Button cell = new Button();
                cell.Width = 50; 
                cell.Height = 50; 
                cell.Foreground = Brushes.White;
                cell.BorderThickness = new Thickness(0); 
                cell.CornerRadius = new CornerRadius(0);
                cell.FontFamily = "Lucida Console"; 
                cell.FontSize = 36;
                cell.HorizontalContentAlignment = HorizontalAlignment.Center; 
                cell.VerticalContentAlignment = VerticalAlignment.Center;
                cell.Click += CellClicked;
                
                string cellName = chrow[j] + (8 - i).ToString();
                cell.Name = cellName;
                
                if ((i + j) % 2 == 0)
                    cell.Background = Brush.Parse("#323232");
                else
                    cell.Background = Brush.Parse("#282828");
                
                FillCells(cell, cellName);
                
                CellsWrap.Children.Add(cell);
                boardCells[cellName] = cell;
                originalContent[cellName] = cell.Content?.ToString() ?? "";
            }
        }
    }
    
    private void FillCells(Button cell, string cellName)
    {
        if (cellName == "a1" || cellName == "h1")
            cell.Content = "♜";
        else if (cellName == "b1" || cellName == "g1")
            cell.Content = "♞";
        else if (cellName == "c1" || cellName == "f1")
            cell.Content = "♝";
        else if (cellName == "d1")
            cell.Content = "♛";
        else if (cellName == "e1")
            cell.Content = "♚";
        else if (cellName[1] == '2')
            cell.Content = "♟";
        
        else if (cellName == "a8" || cellName == "h8")
            cell.Content = "♖";
        else if (cellName == "b8" || cellName == "g8")
            cell.Content = "♘";
        else if (cellName == "c8" || cellName == "f8")
            cell.Content = "♗";
        else if (cellName == "d8")
            cell.Content = "♕";
        else if (cellName == "e8")
            cell.Content = "♔";
        else if (cellName[1] == '7')
            cell.Content = "♙";
    }

    private void CellClicked(object? sender, RoutedEventArgs e)
    {
        if (!isPlayerTurn) return;
        
        Button clickedCell = (Button)sender;
        
        if (selectedCell == null)
        {
            if (clickedCell.Content != null && IsPlayerPiece(clickedCell.Content.ToString()))
            {
                selectedCell = clickedCell;
                HighlightMoves(clickedCell);
                clickedCell.Background = Brush.Parse("#ff0040");
            }
        }
        else
        {
            if (selectedCell != clickedCell && IsValidMove(selectedCell, clickedCell))
            {
                ExecuteMove(selectedCell, clickedCell);
                ClearHighlightsAndRestoreContent();
                selectedCell = null;
                isPlayerTurn = false;
                btimer.Start();
            }
            else
            {
                ClearHighlightsAndRestoreContent();
                selectedCell = null;
            }
        }
    }
    
    private bool IsPlayerPiece(string piece)
    {
        return piece == "♜" || piece == "♞" || piece == "♝" || 
               piece == "♛" || piece == "♚" || piece == "♟";
    }
    
    private bool IsBotPiece(string piece)
    {
        return piece == "♖" || piece == "♘" || piece == "♗" || 
               piece == "♕" || piece == "♔" || piece == "♙";
    }
    
    private void HighlightMoves(Button cell)
    {
        string cellName = cell.Name;
        string piece = cell.Content?.ToString();
        List<(string move, bool isCapture)> possibleMoves = GetPossibleCaptureMoves(cellName, piece);
        
        foreach (var moveInfo in possibleMoves)
        {
            if (boardCells.ContainsKey(moveInfo.move))
            {
                Button targetCell = boardCells[moveInfo.move];
                
                if (!originalContent.ContainsKey(moveInfo.move))
                {
                    originalContent[moveInfo.move] = targetCell.Content?.ToString() ?? "";
                }
                
                if (moveInfo.isCapture)
                {
                    targetCell.Background = Brush.Parse("#676767");
                }
                else
                {
                    targetCell.Background = Brush.Parse("#525252");
                }
            }
        }
    }
    
    private List<(string move, bool isCapture)> GetPossibleCaptureMoves(string cellName, string piece)
    {
        List<(string move, bool isCapture)> moves = new List<(string, bool)>();
        int col = cellName[0] - 'a';
        int row = int.Parse(cellName[1].ToString()) - 1;
        
        switch (piece)
        {
            case "♟":
                GetPawnMoves(row, col, true, moves);
                break;
            case "♙":
                GetPawnMoves(row, col, false, moves);
                break;
            case "♜":
            case "♖":
                GetRookMoves(row, col, moves);
                break;
            case "♞":
            case "♘":
                GetKnightMoves(row, col, moves);
                break;
            case "♝":
            case "♗":
                GetBishopMoves(row, col, moves);
                break;
            case "♛":
            case "♕":
                GetRookMoves(row, col, moves);
                GetBishopMoves(row, col, moves);
                break;
            case "♚":
            case "♔":
                GetKingMoves(row, col, moves);
                break;
        }
        
        return moves;
    }
    
    private void GetPawnMoves(int row, int col, bool isWhite, List<(string move, bool isCapture)> moves)
    {
        int direction = isWhite ? 1 : -1;
        int startRow = isWhite ? 1 : 6;
        
        if (IsValidCell(row + direction, col) && IsEmpty(row + direction, col))
        {
            moves.Add((GetCellName(row + direction, col), false));
            
            if (row == startRow && IsEmpty(row + 2 * direction, col))
            {
                moves.Add((GetCellName(row + 2 * direction, col), false));
            }
        }
        
        int[] attackCols = { col - 1, col + 1 };
        foreach (int attackCol in attackCols)
        {
            if (IsValidCell(row + direction, attackCol) && !IsEmpty(row + direction, attackCol))
            {
                string targetPiece = boardCells[GetCellName(row + direction, attackCol)].Content?.ToString();
                if ((isWhite && IsBotPiece(targetPiece)) || (!isWhite && IsPlayerPiece(targetPiece)))
                {
                    moves.Add((GetCellName(row + direction, attackCol), true));
                }
            }
        }

        if (enPassantTarget != null)
        {
            string target = GetCellName(row + direction, col + 1);
            if (target == enPassantTarget)
                moves.Add((target, true));
            target = GetCellName(row + direction, col - 1);
            if (target == enPassantTarget)
                moves.Add((target, true));
        }
    }
    
    private void GetRookMoves(int row, int col, List<(string move, bool isCapture)> moves)
    {
        AddLinearMoves(row, col, 1, 0, moves);
        AddLinearMoves(row, col, -1, 0, moves);
        AddLinearMoves(row, col, 0, 1, moves);
        AddLinearMoves(row, col, 0, -1, moves);
    }
    
    private void GetBishopMoves(int row, int col, List<(string move, bool isCapture)> moves)
    {
        AddLinearMoves(row, col, 1, 1, moves);
        AddLinearMoves(row, col, 1, -1, moves);
        AddLinearMoves(row, col, -1, 1, moves);
        AddLinearMoves(row, col, -1, -1, moves);
    }
    
    private void GetKnightMoves(int row, int col, List<(string move, bool isCapture)> moves)
    {
        int[] knightMoves = { -2, -1, 1, 2 };
        foreach (int dr in knightMoves)
        {
            foreach (int dc in knightMoves)
            {
                if (Math.Abs(dr) != Math.Abs(dc) && IsValidCell(row + dr, col + dc))
                {
                    if (IsEmpty(row + dr, col + dc))
                    {
                        moves.Add((GetCellName(row + dr, col + dc), false));
                    }
                    else if (IsEnemyPiece(row + dr, col + dc))
                    {
                        moves.Add((GetCellName(row + dr, col + dc), true));
                    }
                }
            }
        }
    }
    
    private void GetKingMoves(int row, int col, List<(string move, bool isCapture)> moves)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                if (IsValidCell(row + dr, col + dc))
                {
                    if (IsEmpty(row + dr, col + dc))
                    {
                        moves.Add((GetCellName(row + dr, col + dc), false));
                    }
                    else if (IsEnemyPiece(row + dr, col + dc))
                    {
                        moves.Add((GetCellName(row + dr, col + dc), true));
                    }
                }
            }
        }
    }
    
    private void AddLinearMoves(int row, int col, int dr, int dc, List<(string move, bool isCapture)> moves)
    {
        int r = row + dr;
        int c = col + dc;
        
        while (IsValidCell(r, c))
        {
            if (IsEmpty(r, c))
            {
                moves.Add((GetCellName(r, c), false));
                r += dr;
                c += dc;
            }
            else if (IsEnemyPiece(r, c))
            {
                moves.Add((GetCellName(r, c), true));
                break;
            }
            else
            {
                break;
            }
        }
    }
    
    private List<string> GetPossibleMoves(string cellName, string piece)
    {
        List<string> moves = new List<string>();
        var movesWithCapture = GetPossibleCaptureMoves(cellName, piece);
        
        foreach (var moveInfo in movesWithCapture)
        {
            moves.Add(moveInfo.move);
        }
        
        return moves;
    }
    
    private bool IsValidMove(Button from, Button to)
    {
        string fromName = from.Name;
        string toName = to.Name;
        string piece = from.Content?.ToString();
        
        List<string> possibleMoves = GetPossibleMoves(fromName, piece);
        return possibleMoves.Contains(toName);
    }
    
    private void ExecuteMove(Button from, Button to)
    {
        string piece = from.Content?.ToString();
        if ((piece == "♟" || piece == "♙") && to.Name == enPassantTarget)
        {
            string capturedPawnCell = GetCellName(to.Name[0] - 'a', int.Parse(to.Name[1].ToString()) - 1 - (piece == "♟" ? 1 : -1));
            boardCells[capturedPawnCell].Content = "";
        }
        
        to.Content = from.Content;
        from.Content = "";
        
        enPassantTarget = null;
        if ((piece == "♟" || piece == "♙") && Math.Abs(int.Parse(to.Name[1].ToString()) - int.Parse(from.Name[1].ToString())) == 2)
        {
            enPassantTarget = GetCellName(from.Name[0] - 'a', (int.Parse(from.Name[1].ToString()) + int.Parse(to.Name[1].ToString())) / 2);
        }
        
        if (piece == "♟" && to.Name[1] == '8')
            to.Content = "♛";
        if (piece == "♙" && to.Name[1] == '1')
            to.Content = "♕";
        
        originalContent[from.Name] = "";
        originalContent[to.Name] = to.Content?.ToString() ?? "";
    }
    
    private bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }
    
    private bool IsEmpty(int row, int col)
    {
        return boardCells[GetCellName(row, col)].Content == null || 
               boardCells[GetCellName(row, col)].Content.ToString() == "";
    }
    
    private bool IsEnemyPiece(int row, int col)
    {
        string piece = boardCells[GetCellName(row, col)].Content?.ToString();
        if (string.IsNullOrEmpty(piece)) return false;
        
        bool isPlayerPiece = IsPlayerPiece(piece);
        return isPlayerPiece != isPlayerTurn;
    }
    
    private string GetCellName(int row, int col)
    {
        char colChar = (char)('a' + col);
        return $"{colChar}{row + 1}";
    }
    
    private void ClearHighlightsAndRestoreContent()
    {
        foreach (var cell in boardCells.Values)
        {
            string cellName = cell.Name;
            int row = 8 - int.Parse(cellName[1].ToString());
            int col = cellName[0] - 'a';
            
            if ((row + col) % 2 == 0)
                cell.Background = Brush.Parse("#323232");
            else
                cell.Background = Brush.Parse("#282828");
            
            if (originalContent.ContainsKey(cellName))
            {
                cell.Content = originalContent[cellName];
            }
        }
        
        if (selectedCell != null)
        {
            string cellName = selectedCell.Name;
            int row = 8 - int.Parse(cellName[1].ToString());
            int col = cellName[0] - 'a';
            
            if ((row + col) % 2 == 0)
                selectedCell.Background = Brush.Parse("#323232");
            else
                selectedCell.Background = Brush.Parse("#282828");
        }
    }

    private void PlayerTimerOut(object sender, EventArgs e)
    {
        if (ptscounter > 0)
        {
            ptscounter--;
            if (ptscounter == 0 && ptmcounter > 0)
            {
                ptmcounter--;
                ptscounter = 59;
                if (ptmcounter == 0)
                {
                    ptimer.Stop();
                    ShowGameOver("Игрок проиграл по времени!");
                }
            }
            UpdateTimerDisplay();
        }
    }
    
    private void BotTimerOut(object sender, EventArgs e)
    {
        if (btscounter > 0)
        {
            btscounter--;
            if (btscounter == 0 && btmcounter > 0)
            {
                btmcounter--;
                btscounter = 59;
                if (btmcounter == 0)
                {
                    btimer.Stop();
                    ShowGameOver("Бот проиграл по времени!");
                }
            }
            UpdateTimerDisplay();
        }
    }
    
    private void UpdateTimerDisplay()
    {
        PlayerTimerLabel.Content = $"{ptmcounter:00}:{ptscounter:00}";
        BotTimerLabel.Content = $"{btmcounter:00}:{btscounter:00}";
    }
    
    private void ShowGameOver(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var dialog = new Window
            {
                Title = "Конец игры",
                Width = 300,
                Height = 150,
                Content = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    FontFamily = "Lucida Console"
                }
            };
            dialog.ShowDialog(this);
        });
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}