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
    
    // Словари для хранения взятых фигур
    private List<Label> deadWhitePieces = new List<Label>();
    private List<Label> deadBlackPieces = new List<Label>();
    
    public LocalGameWindow()
    {
        InitializeComponent();
        InitializeBoard();
        InitializeDeadPiecesLists();
        
        p1timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        p1timer.Tick += new EventHandler(Player1TimerOut);
        p1timer.Start();
        
        p2timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
        p2timer.Tick += new EventHandler(Player2TimerOut);
        
        UpdateTimerDisplay();
    }
    
    private void InitializeDeadPiecesLists()
    {
        // Собираем все Label из deadfigbstack (черные фигуры)
        var blackStack = this.FindControl<StackPanel>("deadfigbstack");
        if (blackStack != null)
        {
            foreach (var child in blackStack.Children)
            {
                if (child is Label label)
                {
                    deadBlackPieces.Add(label);
                    label.Foreground = Brush.Parse("#565656");
                }
            }
        }
        
        // Собираем все Label из deadfigwstack (белые фигуры)
        var whiteStack = this.FindControl<StackPanel>("deadfigwstack");
        if (whiteStack != null)
        {
            foreach (var child in whiteStack.Children)
            {
                if (child is Label label)
                {
                    deadWhitePieces.Add(label);
                    label.Foreground = Brush.Parse("#565656");
                }
            }
        }
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
            // Выбор фигуры
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
            // Попытка сделать ход
            if (selectedCell != clickedCell && IsValidMove(selectedCell, clickedCell))
            {
                // Проверяем, не будет ли король под шахом после хода
                if (!WouldBeCheckAfterMove(selectedCell, clickedCell))
                {
                    // Выполняем ход
                    ExecuteMove(selectedCell, clickedCell);
                    ClearHighlights();
                    selectedCell = null;
                    
                    // Проверяем, не поставил ли текущий игрок шах/мат противнику
                    bool isOpponentInCheck = IsKingInCheck(!isPlayer1Turn);
                    
                    if (isOpponentInCheck)
                    {
                        // Проверяем, есть ли у противника ходы
                        if (IsCheckmate(!isPlayer1Turn))
                        {
                            string winner = isPlayer1Turn ? "White" : "Black";
                            ShowGameOver($"Checkmate! {winner} wins!");
                            return;
                        }
                        else
                        {
                            ShowNotification("CHECK!");
                        }
                    }
                    
                    // Смена хода
                    isPlayer1Turn = !isPlayer1Turn;
                    
                    // Управление таймерами
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
    
    // Проверяет, будет ли король под шахом после хода
    private bool WouldBeCheckAfterMove(Button from, Button to)
    {
        // Сохраняем текущее состояние
        string fromPiece = from.Content?.ToString() ?? "";
        string toPiece = to.Content?.ToString() ?? "";
        
        Button currentKing = isPlayer1Turn ? P1KingCell : P2KingCell;
        Button? oldKingPosition = null;
        
        // Если перемещаем короля, запоминаем его старую позицию
        if (fromPiece == (isPlayer1Turn ? "♚" : "♔"))
        {
            oldKingPosition = currentKing;
            currentKing = to;
        }
        
        // Временно выполняем ход
        from.Content = "";
        to.Content = fromPiece;
        
        // Проверяем, под шахом ли король
        bool isInCheck = IsSquareAttacked(currentKing.Name, isPlayer1Turn);
        
        // Откатываем ход
        from.Content = fromPiece;
        to.Content = toPiece;
        
        return isInCheck;
    }
    
    // Проверяет, находится ли король под шахом
    private bool IsKingInCheck(bool isWhiteKing)
    {
        Button kingCell = isWhiteKing ? P1KingCell : P2KingCell;
        return IsSquareAttacked(kingCell.Name, isWhiteKing);
    }
    
    // Проверяет, атакована ли клетка
    private bool IsSquareAttacked(string square, bool isWhiteKing)
    {
        int col = square[0] - 'a';
        int row = int.Parse(square[1].ToString()) - 1;
        
        // Проверяем все фигуры противника
        foreach (var cell in boardCells.Values)
        {
            if (cell.Content != null && !string.IsNullOrEmpty(cell.Content.ToString()))
            {
                string piece = cell.Content.ToString();
                bool isEnemy = isWhiteKing ? IsPlayer2Piece(piece) : IsPlayer1Piece(piece);
                
                if (isEnemy)
                {
                    // Получаем все возможные ходы вражеской фигуры
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
    
    // Проверяет, мат ли это
    private bool IsCheckmate(bool isWhiteKing)
    {
        // Сначала проверяем, есть ли шах
        if (!IsKingInCheck(isWhiteKing))
            return false;
        
        // Ищем любой безопасный ход
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
                            
                            // Сохраняем состояние
                            string fromPiece = cell.Content?.ToString() ?? "";
                            string toPiece = toCell.Content?.ToString() ?? "";
                            Button oldKing = isWhiteKing ? P1KingCell : P2KingCell;
                            Button newKing = oldKing;
                            
                            // Если перемещаем короля
                            if (fromPiece == (isWhiteKing ? "♚" : "♔"))
                            {
                                newKing = toCell;
                            }
                            
                            // Временно выполняем ход
                            cell.Content = "";
                            toCell.Content = fromPiece;
                            
                            // Проверяем, под шахом ли король
                            bool isSafe = !IsSquareAttacked(newKing.Name, isWhiteKing);
                            
                            // Откатываем
                            cell.Content = fromPiece;
                            toCell.Content = toPiece;
                            
                            if (isSafe)
                            {
                                return false; // Нашли безопасный ход
                            }
                        }
                    }
                }
            }
        }
        
        return true; // Нет безопасных ходов
    }
    
    // Получает все возможные ходы для фигуры (без проверки на безопасность)
    private List<string> GetAllMovesForPiece(string cellName, string piece)
    {
        List<string> moves = new List<string>();
        int col = cellName[0] - 'a';
        int row = int.Parse(cellName[1].ToString()) - 1;
        
        switch (piece)
        {
            case "♟": GetPawnMoves(row, col, true, moves); break;
            case "♙": GetPawnMoves(row, col, false, moves); break;
            case "♜": case "♖": GetRookMoves(row, col, moves); break;
            case "♞": case "♘": GetKnightMoves(row, col, moves); break;
            case "♝": case "♗": GetBishopMoves(row, col, moves); break;
            case "♛": case "♕": 
                GetRookMoves(row, col, moves);
                GetBishopMoves(row, col, moves);
                break;
            case "♚": case "♔": GetKingMoves(row, col, moves); break;
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
                
                // Подсвечиваем только безопасные ходы
                if (!WouldBeCheckAfterMove(cell, targetCell))
                {
                    if (targetCell.Content != null && !string.IsNullOrEmpty(targetCell.Content.ToString()))
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
    }
    
    private void GetPawnMoves(int row, int col, bool isWhite, List<string> moves)
    {
        int direction = isWhite ? 1 : -1;
        int startRow = isWhite ? 1 : 6;
        
        // Ход вперед
        if (IsValidCell(row + direction, col) && IsEmpty(row + direction, col))
        {
            moves.Add(GetCellName(row + direction, col));
            
            // Первый ход на две клетки
            if (row == startRow && IsEmpty(row + 2 * direction, col))
            {
                moves.Add(GetCellName(row + 2 * direction, col));
            }
        }
        
        // Взятие
        int[] attackCols = { col - 1, col + 1 };
        foreach (int attackCol in attackCols)
        {
            if (IsValidCell(row + direction, attackCol) && !IsEmpty(row + direction, attackCol))
            {
                string targetPiece = boardCells[GetCellName(row + direction, attackCol)].Content?.ToString();
                if ((isWhite && IsPlayer2Piece(targetPiece)) || (!isWhite && IsPlayer1Piece(targetPiece)))
                {
                    moves.Add(GetCellName(row + direction, attackCol));
                }
            }
        }
        
        // Взятие на проходе
        if (enPassantTarget != null)
        {
            string target = GetCellName(row + direction, col + 1);
            if (target == enPassantTarget)
                moves.Add(target);
            target = GetCellName(row + direction, col - 1);
            if (target == enPassantTarget)
                moves.Add(target);
        }
    }
    
    private void GetRookMoves(int row, int col, List<string> moves)
    {
        AddLinearMoves(row, col, 1, 0, moves);
        AddLinearMoves(row, col, -1, 0, moves);
        AddLinearMoves(row, col, 0, 1, moves);
        AddLinearMoves(row, col, 0, -1, moves);
    }
    
    private void GetBishopMoves(int row, int col, List<string> moves)
    {
        AddLinearMoves(row, col, 1, 1, moves);
        AddLinearMoves(row, col, 1, -1, moves);
        AddLinearMoves(row, col, -1, 1, moves);
        AddLinearMoves(row, col, -1, -1, moves);
    }
    
    private void GetKnightMoves(int row, int col, List<string> moves)
    {
        int[] dr = { -2, -2, -1, -1, 1, 1, 2, 2 };
        int[] dc = { -1, 1, -2, 2, -2, 2, -1, 1 };
        
        for (int i = 0; i < 8; i++)
        {
            int newRow = row + dr[i];
            int newCol = col + dc[i];
            
            if (IsValidCell(newRow, newCol))
            {
                if (IsEmpty(newRow, newCol) || IsEnemyPiece(newRow, newCol))
                {
                    moves.Add(GetCellName(newRow, newCol));
                }
            }
        }
    }
    
    private void GetKingMoves(int row, int col, List<string> moves)
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
                    if (IsEmpty(newRow, newCol) || IsEnemyPiece(newRow, newCol))
                    {
                        moves.Add(GetCellName(newRow, newCol));
                    }
                }
            }
        }
    }
    
    private void AddLinearMoves(int row, int col, int dr, int dc, List<string> moves)
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
            else if (IsEnemyPiece(r, c))
            {
                moves.Add(GetCellName(r, c));
                break;
            }
            else
            {
                break;
            }
        }
    }
    
    private void AddCapturedPiece(string piece)
    {
        // Белые фигуры (P1) - черные символы
        if (IsPlayer1Piece(piece))
        {
            foreach (var label in deadBlackPieces)
            {
                if (label.Content?.ToString() == piece && label.Foreground == Brush.Parse("#565656"))
                {
                    label.Foreground = Brushes.White;
                    break;
                }
            }
        }
        // Черные фигуры (P2) - белые символы
        else if (IsPlayer2Piece(piece))
        {
            foreach (var label in deadWhitePieces)
            {
                if (label.Content?.ToString() == piece && label.Foreground == Brush.Parse("#565656"))
                {
                    label.Foreground = Brushes.White;
                    break;
                }
            }
        }
    }
    
    private void ExecuteMove(Button from, Button to)
    {
        string piece = from.Content?.ToString();
        
        // Сохраняем взятую фигуру для отображения
        string capturedPiece = to.Content?.ToString();
        if (!string.IsNullOrEmpty(capturedPiece))
        {
            AddCapturedPiece(capturedPiece);
        }
        
        // Взятие на проходе
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
        
        // Выполняем ход
        to.Content = from.Content;
        from.Content = "";
        
        // Обновляем en passant target
        enPassantTarget = null;
        if ((piece == "♟" || piece == "♙") && Math.Abs(int.Parse(to.Name[1].ToString()) - int.Parse(from.Name[1].ToString())) == 2)
        {
            enPassantTarget = GetCellName(from.Name[0] - 'a', (int.Parse(from.Name[1].ToString()) + int.Parse(to.Name[1].ToString())) / 2);
        }
        
        // Превращение пешки
        if (piece == "♟" && to.Name[1] == '8')
            to.Content = "♛";
        if (piece == "♙" && to.Name[1] == '1')
            to.Content = "♕";
        
        // Обновляем позицию короля
        if (piece == "♚")
            P1KingCell = to;
        if (piece == "♔")
            P2KingCell = to;
    }
    
    private bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }
    
    private bool IsEmpty(int row, int col)
    {
        string cellName = GetCellName(row, col);
        return boardCells[cellName].Content == null || 
               boardCells[cellName].Content.ToString() == "";
    }
    
    private bool IsEnemyPiece(int row, int col)
    {
        string piece = boardCells[GetCellName(row, col)].Content?.ToString();
        if (string.IsNullOrEmpty(piece)) return false;
        
        bool isPlayerPiece = IsPlayer1Piece(piece);
        return isPlayerPiece != isPlayer1Turn;
    }
    
    private string GetCellName(int row, int col)
    {
        char colChar = (char)('a' + col);
        return $"{colChar}{row + 1}";
    }
    
    private void ClearHighlights()
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
        }
    }
    
    private void Player1TimerOut(object sender, EventArgs e)
    {
        if (p1tscounter > 0)
        {
            p1tscounter--;
        }
        else
        {
            if (p1tmcounter > 0)
            {
                p1tmcounter--;
                p1tscounter = 59;
            }
        }
        
        UpdateTimerDisplay();
        
        if (p1tmcounter == 0 && p1tscounter == 0)
        {
            p1timer.Stop();
            ShowGameOver("Player 1 (Black) lost on time!");
        }
    }
    
    private void Player2TimerOut(object sender, EventArgs e)
    {
        if (p2tscounter > 0)
        {
            p2tscounter--;
        }
        else
        {
            if (p2tmcounter > 0)
            {
                p2tmcounter--;
                p2tscounter = 59;
            }
        }
        
        UpdateTimerDisplay();
        
        if (p2tmcounter == 0 && p2tscounter == 0)
        {
            p2timer.Stop();
            ShowGameOver("Player 2 (White) lost on time!");
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
            var titleLabel = this.FindControl<Label>("Title");
            if (titleLabel != null)
            {
                string originalTitle = titleLabel.Content?.ToString() ?? "Chess";
                titleLabel.Content = message;
                var timer = new System.Timers.Timer(2000);
                timer.Elapsed += (s, e) => 
                {
                    Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        if (titleLabel != null)
                            titleLabel.Content = originalTitle;
                    });
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        });
    }
    
    private void LeaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}