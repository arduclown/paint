namespace GraphicEditor.Common.Models;

public enum ToolType
{
    Select,
    Circle,
    Rectangle,
    Triangle,
    Line
}

public record ToolInfo(ToolType Type, string DisplayName, string Hotkey);
