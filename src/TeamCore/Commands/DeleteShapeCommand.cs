using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

/// <summary>Команда удаления фигуры (с запоминанием позиции для Undo).</summary>
public class DeleteShapeCommand(ISceneCollection collection, ISceneShape shape) : IEditorCommand
{
    // Позиция фигуры на момент удаления — нужна для восстановления
    private int _index = -1;

    public void Execute()
    {
        _index = collection.IndexOf(shape);
        if (_index >= 0) collection.RemoveAt(_index);
    }

    public void Undo()
    {
        if (_index >= 0) collection.Insert(_index, shape);
    }
}
