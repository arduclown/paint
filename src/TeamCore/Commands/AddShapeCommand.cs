using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

/// <summary>Команда добавления фигуры на сцену.</summary>
public class AddShapeCommand(ISceneCollection collection, ISceneShape shape) : IEditorCommand
{
    public void Execute() => collection.Add(shape);
    public void Undo() => collection.Remove(shape);
}
