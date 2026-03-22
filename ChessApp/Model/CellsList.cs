using System.Collections.Generic;
using Avalonia.Controls;

namespace ChessApp.Model;

public static class CellsList
{
    // Изменено с cells на Cells для соответствия соглашению об именовании
    public static List<Cell> Cells { get; } = new List<Cell>();
    
    // Вспомогательные методы
    public static void Clear()
    {
        Cells.Clear();
    }
    
    public static Cell? GetCell(string position)
    {
        return Cells.Find(c => c.Content?.ToString() == position);
    }
}