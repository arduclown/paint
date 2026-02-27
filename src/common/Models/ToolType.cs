namespace GraphicEditor.Common.Models;

public enum ToolType
{
    Select,
    Circle,
    Rectangle,
    Triangle,
    Line
}

/// <summary>Описание инструмента для UI (имя + горячая клавиша).</summary>
public record ToolInfo(ToolType Type, string DisplayName, string Hotkey);
