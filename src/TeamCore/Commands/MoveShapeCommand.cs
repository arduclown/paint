using Avalonia;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class MoveShapeCommand(ISceneShape shape, Point delta) : IEditorCommand
{
    public void Execute() => shape.Move(delta);
    public void Undo() => shape.Move(new Point(-delta.X, -delta.Y));
}
