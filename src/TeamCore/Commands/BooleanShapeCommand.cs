using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore.Commands;

public class BooleanShapeCommand : IEditorCommand
{
    private readonly ISceneCollection _collection;
    private readonly ISceneShape _shape1;
    private readonly ISceneShape _shape2;
    private readonly ISceneShape _combined;
    private int _idx1;
    private int _idx2;

    public BooleanShapeCommand(ISceneCollection collection,
        ISceneShape shape1, ISceneShape shape2, ISceneShape combined)
    {
        _collection = collection;
        _shape1 = shape1;
        _shape2 = shape2;
        _combined = combined;
    }

    public void Execute()
    {
        _idx1 = _collection.IndexOf(_shape1);
        _idx2 = _collection.IndexOf(_shape2);
        _collection.Remove(_shape1);
        _collection.Remove(_shape2);
        _collection.Add(_combined);
    }

    public void Undo()
    {
        _collection.Remove(_combined);
        // Вставляем в порядке возрастания индексов, чтобы позиции были корректны
        if (_idx1 <= _idx2)
        {
            _collection.Insert(_idx1, _shape1);
            _collection.Insert(_idx2, _shape2);
        }
        else
        {
            _collection.Insert(_idx2, _shape2);
            _collection.Insert(_idx1, _shape1);
        }
    }
}
