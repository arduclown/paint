using Avalonia.Media;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

/// <summary>Команда смены цвета заливки и обводки (с запоминанием старых значений).</summary>
public class ChangeStyleCommand(ISceneShape shape, Color newFill, Color newStroke) : IEditorCommand
{
    // Сохраняем прежние цвета для отмены
    private readonly Color _oldFill = shape.FillColor;
    private readonly Color _oldStroke = shape.StrokeColor;

    public void Execute()
    {
        shape.FillColor = newFill;
        shape.StrokeColor = newStroke;
    }

    public void Undo()
    {
        shape.FillColor = _oldFill;
        shape.StrokeColor = _oldStroke;
    }
}
