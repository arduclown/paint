using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class DeleteShapeCommand(ISceneCollection collection, ISceneShape shape) : IEditorCommand
{
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
