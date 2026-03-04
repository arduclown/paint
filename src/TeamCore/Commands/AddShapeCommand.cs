using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class AddShapeCommand(ISceneCollection collection, ISceneShape shape) : IEditorCommand
{
    public void Execute() => collection.Add(shape);
    public void Undo() => collection.Remove(shape);
}
