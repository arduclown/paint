using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore.Commands
{
    public class DeleteShapeCommand : IEditorCommand
    {
        private readonly ISceneCollection _collection;
        private readonly ISceneShape _shape;
        private int _index = -1;

        public DeleteShapeCommand(ISceneCollection collection, ISceneShape shape)
        {
            _collection = collection;
            _shape = shape;
        }

        public void Execute()
        {
            _index = _collection.IndexOf(_shape);
            if (_index >= 0) _collection.RemoveAt(_index);
        }

        public void Undo()
        {
            if (_index >= 0) _collection.Insert(_index, _shape);
        }
    }
}
