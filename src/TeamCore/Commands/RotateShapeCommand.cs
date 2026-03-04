using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class RotateShapeCommand(ISceneShape shape, double angle) : IEditorCommand
{
    public void Execute() => shape.Rotate(angle);
    public void Undo() => shape.Rotate(-angle);
}
