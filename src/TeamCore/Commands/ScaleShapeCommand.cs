using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class ScaleShapeCommand(ISceneShape shape, double ratio) : IEditorCommand
{
    public void Execute() => shape.Scale(ratio);
    public void Undo() => shape.Scale(1.0 / ratio);
}
