using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;

namespace ChessApp;

public partial class LocalGameWindow : Window
{
    DispatcherTimer p1timer = new DispatcherTimer();
    public int p1tscounter = 0;
    public int p1tmcounter = 10;
    DispatcherTimer p2timer = new DispatcherTimer();
    public int p2tscounter = 0;
    public int p2tmcounter = 10;
    
    private Button? selectedCell = null;
    private Button P1KingCell = null;
    private Button P2KingCell = null;
    private bool isPlayer1Turn = true;
    private Dictionary<string, Button> boardCells = new Dictionary<string, Button>();
    private string? enPassantTarget = null;
    
    private StackPanel deadBlackPanel;
    private StackPanel deadWhitePanel;
    
    public LocalGameWindow()
    {
        InitializeComponent();
        
        deadBlackPanel = this.FindControl<StackPanel>("deadfigbstack");
        deadWhitePanel = this.FindControl<StackPanel>("deadfigwstack");
        
        deadBlackPanel.Children.Clear();
        deadWhitePanel.Children.Clear();
        
        InitializeBoard();
        
        p1timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        p1timer.Tick += new EventHandler(Player1TimerOut);
        p1timer.Start();
        
        p2timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        p2timer.Tick += new EventHandler(Player2TimerOut);
        
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
        {
            cell.Content = "♚";
            P1KingCell = cell;
        }
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
        {
            cell.Content = "♔";
            P2KingCell = cell;
        }
        else if (cellName[1] == '7')
            cell.Content = "♙";
    }

    private void CellClicked(object? sender, RoutedEventArgs e)
    {
        Button clickedCell = (Button)sender;
        
        if (selectedCell == null)
        {
            if (clickedCell.Content != null && !string.IsNullOrEmpty(clickedCell.Content.ToString()))
            {
                string piece = clickedCell.Content.ToString();
                bool isCorrectPiece = isPlayer1Turn ? IsPlayer1Piece(piece) : IsPlayer2Piece(piece);
                
                if (isCorrectPiece)
                {
                    selectedCell = clickedCell;
                    HighlightMoves(clickedCell);
                    clickedCell.Background = Brush.Parse("#ff0040");
                }
            }
        }
        else
        {
            if (selectedCell != clickedCell && IsValidMove(selectedCell, clickedCell))
            {
                if (!WouldBeCheckAfterMove(selectedCell, clickedCell))
                {
                    ExecuteMove(selectedCell, clickedCell);
                    ClearHighlights();
                    selectedCell = null;
                    
                    bool isOpponentInCheck = IsKingInCheck(!isPlayer1Turn);
                    
                    if (isOpponentInCheck)
                    {
                        if (IsCheckmate(!isPlayer1Turn))
                        {
                            string looser = isPlayer1Turn ? "Black" : "White";
                            ShowGameOver($"Check and mate, {looser}...");
                            return;
                        }
                        else
                        {
                            ShowNotification("CHECK!");
                        }
                    }
                    
                    isPlayer1Turn = !isPlayer1Turn;
                    
                    if (isPlayer1Turn)
                    {
                        p1timer.Start();
                        p2timer.Stop();
                    }
                    else
                    {
                        p2timer.Start();
                        p1timer.Stop();
                    }
                }
                else
                {
                    ClearHighlights();
                    selectedCell = null;
                    ShowNotification("Invalid move: King would be in check!");
                }
            }
            else
            {
                ClearHighlights();
                selectedCell = null;
            }
        }
    }
    
    private bool WouldBeCheckAfterMove(Button from, Button to)
    {
        string fromPiece = from.Content?.ToString() ?? "";
        string toPiece = to.Content?.ToString() ?? "";
        
        Button currentKing = isPlayer1Turn ? P1KingCell : P2KingCell;
        bool isWhiteKing = isPlayer1Turn;
        
        if (fromPiece == (isPlayer1Turn ? "♚" : "♔"))
        {
            currentKing = to;
        }
        
        from.Content = "";
        to.Content = fromPiece;
        
        bool isInCheck = IsSquareAttacked(currentKing.Name, isWhiteKing);
        
        from.Content = fromPiece;
        to.Content = toPiece;
        
        return isInCheck;
    }
    
    private bool IsKingInCheck(bool isWhiteKing)
    {
        Button kingCell = isWhiteKing ? P1KingCell : P2KingCell;
        return IsSquareAttacked(kingCell.Name, isWhiteKing);
    }
    
    private bool IsSquareAttacked(string square, bool isWhiteKing)
    {
        foreach (var cell in boardCells.Values)
        {
            if (cell.Content != null && !string.IsNullOrEmpty(cell.Content.ToString()))
            {
                string piece = cell.Content.ToString();
                bool isEnemy = isWhiteKing ? IsPlayer2Piece(piece) : IsPlayer1Piece(piece);
                
                if (isEnemy)
                {
                    List<string> moves = GetAllMovesForPiece(cell.Name, piece);
                    if (moves.Contains(square))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    private bool IsCheckmate(bool isWhiteKing)
    {
        if (!IsKingInCheck(isWhiteKing))
            return false;
        
        foreach (var cell in boardCells.Values)
        {
            if (cell.Content != null && !string.IsNullOrEmpty(cell.Content.ToString()))
            {
                string piece = cell.Content.ToString();
                bool isCorrectPiece = isWhiteKing ? IsPlayer1Piece(piece) : IsPlayer2Piece(piece);
                
                if (isCorrectPiece)
                {
                    List<string> moves = GetAllMovesForPiece(cell.Name, piece);
                    
                    foreach (string move in moves)
                    {
                        if (boardCells.ContainsKey(move))
                        {
                            Button toCell = boardCells[move];
                            
                            string fromPiece = cell.Content?.ToString() ?? "";
                            string toPiece = toCell.Content?.ToString() ?? "";
                            
                            Button currentKing = isWhiteKing ? P1KingCell : P2KingCell;
                            Button newKing = currentKing;
                            
                            if (fromPiece == (isWhiteKing ? "♚" : "♔"))
                            {
                                newKing = toCell;
                            }
                            
                            cell.Content = "";
                            toCell.Content = fromPiece;
                            
                            bool isSafe = !IsSquareAttacked(newKing.Name, isWhiteKing);
                            
                            cell.Content = fromPiece;
                            toCell.Content = toPiece;
                            
                            if (isSafe)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
        }
        return true;
    }
    
    private List<string> GetAllMovesForPiece(string cellName, string piece)
    {
        bool isWhitePiece = IsPlayer1Piece(piece);
        List<string> moves = new List<string>();
        int col = cellName[0] - 'a';
        int row = int.Parse(cellName[1].ToString()) - 1;
        
        switch (piece)
        {
            case "♟": GetPawnMoves(row, col, true, moves, isWhitePiece); break;
            case "♙": GetPawnMoves(row, col, false, moves, isWhitePiece); break;
            case "♜": case "♖": GetRookMoves(row, col, moves, isWhitePiece); break;
            case "♞": case "♘": GetKnightMoves(row, col, moves, isWhitePiece); break;
            case "♝": case "♗": GetBishopMoves(row, col, moves, isWhitePiece); break;
            case "♛": case "♕":
                GetRookMoves(row, col, moves, isWhitePiece);
                GetBishopMoves(row, col, moves, isWhitePiece);
                break;
            case "♚": case "♔": GetKingMoves(row, col, moves, isWhitePiece); break;
        }
        return moves;
    }
    
    private bool IsValidMove(Button from, Button to)
    {
        string piece = from.Content?.ToString();
        List<string> moves = GetAllMovesForPiece(from.Name, piece);
        return moves.Contains(to.Name);
    }
    
    private bool IsPlayer1Piece(string piece)
    {
        return piece == "♜" || piece == "♞" || piece == "♝" || 
               piece == "♛" || piece == "♚" || piece == "♟";
    }
    
    private bool IsPlayer2Piece(string piece)
    {
        return piece == "♖" || piece == "♘" || piece == "♗" || 
               piece == "♕" || piece == "♔" || piece == "♙";
    }
    
    private void HighlightMoves(Button cell)
    {
        string piece = cell.Content?.ToString();
        List<string> moves = GetAllMovesForPiece(cell.Name, piece);
        
        foreach (string move in moves)
        {
            if (boardCells.ContainsKey(move))
            {
                Button targetCell = boardCells[move];
                if (!WouldBeCheckAfterMove(cell, targetCell))
                {
                    if (targetCell.Content != null && !string.IsNullOrEmpty(targetCell.Content.ToString()))
                        targetCell.Background = Brush.Parse("#676767");
                    else
                        targetCell.Background = Brush.Parse("#525252");
                }
            }
        }
    }
    
    private void GetPawnMoves(int row, int col, bool isWhite, List<string> moves, bool isWhitePiece)
    {
        int direction = isWhite ? 1 : -1;
        int startRow = isWhite ? 1 : 6;
        
        if (IsValidCell(row + direction, col) && IsEmpty(row + direction, col))
        {
            moves.Add(GetCellName(row + direction, col));
            if (row == startRow && IsEmpty(row + 2 * direction, col))
                moves.Add(GetCellName(row + 2 * direction, col));
        }
        
        int[] attackCols = { col - 1, col + 1 };
        foreach (int attackCol in attackCols)
        {
            if (IsValidCell(row + direction, attackCol) && !IsEmpty(row + direction, attackCol))
            {
                string targetPiece = boardCells[GetCellName(row + direction, attackCol)].Content?.ToString();
                bool isEnemy = isWhitePiece ? IsPlayer2Piece(targetPiece) : IsPlayer1Piece(targetPiece);
                if (isEnemy)
                    moves.Add(GetCellName(row + direction, attackCol));
            }
        }
        
        if (enPassantTarget != null)
        {
            string target = GetCellName(row + direction, col + 1);
            if (target == enPassantTarget) moves.Add(target);
            target = GetCellName(row + direction, col - 1);
            if (target == enPassantTarget) moves.Add(target);
        }
    }
    
    private void GetRookMoves(int row, int col, List<string> moves, bool isWhitePiece)
    {
        AddLinearMoves(row, col, 1, 0, moves, isWhitePiece);
        AddLinearMoves(row, col, -1, 0, moves, isWhitePiece);
        AddLinearMoves(row, col, 0, 1, moves, isWhitePiece);
        AddLinearMoves(row, col, 0, -1, moves, isWhitePiece);
    }
    
    private void GetBishopMoves(int row, int col, List<string> moves, bool isWhitePiece)
    {
        AddLinearMoves(row, col, 1, 1, moves, isWhitePiece);
        AddLinearMoves(row, col, 1, -1, moves, isWhitePiece);
        AddLinearMoves(row, col, -1, 1, moves, isWhitePiece);
        AddLinearMoves(row, col, -1, -1, moves, isWhitePiece);
    }
    
    private void GetKnightMoves(int row, int col, List<string> moves, bool isWhitePiece)
    {
        int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
        int[] dc = { -1, 1, -2, 2, -2, 2, -1, 1 };
        for (int i = 0; i < 8; i++)
        {
            int newRow = row + dr[i];
            int newCol = col + dc[i];
            if (IsValidCell(newRow, newCol))
            {
                if (IsEmpty(newRow, newCol))
                    moves.Add(GetCellName(newRow, newCol));
                else
                {
                    string targetPiece = boardCells[GetCellName(newRow, newCol)].Content?.ToString();
                    bool isEnemy = isWhitePiece ? IsPlayer2Piece(targetPiece) : IsPlayer1Piece(targetPiece);
                    if (isEnemy)
                        moves.Add(GetCellName(newRow, newCol));
                }
            }
        }
    }
    
    private void GetKingMoves(int row, int col, List<string> moves, bool isWhitePiece)
    {
        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int newRow = row + dr;
                int newCol = col + dc;
                if (IsValidCell(newRow, newCol))
                {
                    if (IsEmpty(newRow, newCol))
                        moves.Add(GetCellName(newRow, newCol));
                    else
                    {
                        string targetPiece = boardCells[GetCellName(newRow, newCol)].Content?.ToString();
                        bool isEnemy = isWhitePiece ? IsPlayer2Piece(targetPiece) : IsPlayer1Piece(targetPiece);
                        if (isEnemy)
                            moves.Add(GetCellName(newRow, newCol));
                    }
                }
            }
        }
    }
    
    private void AddLinearMoves(int row, int col, int dr, int dc, List<string> moves, bool isWhitePiece)
    {
        int r = row + dr;
        int c = col + dc;
        while (IsValidCell(r, c))
        {
            if (IsEmpty(r, c))
            {
                moves.Add(GetCellName(r, c));
                r += dr;
                c += dc;
            }
            else
            {
                string targetPiece = boardCells[GetCellName(r, c)].Content?.ToString();
                bool isEnemy = isWhitePiece ? IsPlayer2Piece(targetPiece) : IsPlayer1Piece(targetPiece);
                if (isEnemy)
                    moves.Add(GetCellName(r, c));
                break;
            }
        }
    }
    
    private void AddCapturedPiece(string piece)
    {
        Label newLabel = new Label
        {
            Width = 25,
            Height = 25,
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.White,
            Content = piece
        };
        
        if (IsPlayer1Piece(piece))
        {
            deadWhitePanel.Children.Add(newLabel);
        }
        else if (IsPlayer2Piece(piece))
        {
            deadBlackPanel.Children.Add(newLabel);
        }
    }
    
    private void ExecuteMove(Button from, Button to)
    {
        string piece = from.Content?.ToString();
        
        string capturedPiece = to.Content?.ToString();
        if (!string.IsNullOrEmpty(capturedPiece))
            AddCapturedPiece(capturedPiece);
        
        if ((piece == "♟" || piece == "♙") && to.Name == enPassantTarget)
        {
            int direction = piece == "♟" ? 1 : -1;
            string capturedPawnCell = GetCellName(to.Name[0] - 'a', int.Parse(to.Name[1].ToString()) - 1 - direction);
            string capturedPawn = boardCells[capturedPawnCell].Content?.ToString();
            if (!string.IsNullOrEmpty(capturedPawn))
            {
                AddCapturedPiece(capturedPawn);
                boardCells[capturedPawnCell].Content = "";
            }
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
        
        if (piece == "♚")
            P1KingCell = to;
        if (piece == "♔")
            P2KingCell = to;
    }
    
    private bool IsValidCell(int row, int col) => row >= 0 && row < 8 && col >= 0 && col < 8;
    
    private bool IsEmpty(int row, int col)
    {
        string cellName = GetCellName(row, col);
        return boardCells[cellName].Content == null || boardCells[cellName].Content.ToString() == "";
    }
    
    private bool IsEnemyPiece(int row, int col)
    {
        string piece = boardCells[GetCellName(row, col)].Content?.ToString();
        if (string.IsNullOrEmpty(piece)) return false;
        bool isPlayerPiece = IsPlayer1Piece(piece);
        return isPlayerPiece != isPlayer1Turn;
    }
    
    private string GetCellName(int row, int col) => $"{(char)('a' + col)}{row + 1}";
    
    private void ClearHighlights()
    {
        foreach (var cell in boardCells.Values)
        {
            string cellName = cell.Name;
            int row = 8 - int.Parse(cellName[1].ToString());
            int col = cellName[0] - 'a';
            cell.Background = ((row + col) % 2 == 0) ? Brush.Parse("#323232") : Brush.Parse("#282828");
        }
    }
    
    private void Player1TimerOut(object sender, EventArgs e)
    {
        if (p1tscounter > 0) p1tscounter--;
        else if (p1tmcounter > 0) { p1tmcounter--; p1tscounter = 59; }
        UpdateTimerDisplay();
        if (p1tmcounter == 0 && p1tscounter == 0)
        {
            p1timer.Stop();
            ShowGameOver("Player 1 lost on time!");
        }
    }
    
    private void Player2TimerOut(object sender, EventArgs e)
    {
        if (p2tscounter > 0) p2tscounter--;
        else if (p2tmcounter > 0) { p2tmcounter--; p2tscounter = 59; }
        UpdateTimerDisplay();
        if (p2tmcounter == 0 && p2tscounter == 0)
        {
            p2timer.Stop();
            ShowGameOver("Player 2 lost on time!");
        }
    }
    
    private void UpdateTimerDisplay()
    {
        Player1TimerLabel.Content = $"{p1tmcounter:00}:{p1tscounter:00}";
        Player2TimerLabel.Content = $"{p2tmcounter:00}:{p2tscounter:00}";
    }
    
    private void ShowGameOver(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var dialog = new Window
            {
                Title = "Game Over",
                Width = 300,
                Height = 150,
                Content = new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    Background = Brush.Parse("#232323"),
                    FontFamily = "Lucida Console",
                    Foreground = Brushes.White
                }
            };
            dialog.ShowDialog(this);
        });
    }
    
    private void ShowNotification(string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var titleLabel = this.FindControl<Label>("NotifyLabel");
            if (titleLabel != null)
            {
                string originalTitle = titleLabel.Content?.ToString() ?? "";
                titleLabel.Content = message;
                var timer = new System.Timers.Timer(2000);
                timer.Elapsed += (s, e) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() => titleLabel.Content = originalTitle);
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        });
    }
    
    private void LeaveButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}