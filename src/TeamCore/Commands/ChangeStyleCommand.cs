using Avalonia.Media;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class ChangeStyleCommand(ISceneShape shape, Color newFill, Color newStroke) : IEditorCommand
{
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
