using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore.Commands
{
    public class AddShapeCommand : IEditorCommand
    {
        private readonly ISceneCollection _collection;
        private readonly ISceneShape _shape;

        public AddShapeCommand(ISceneCollection collection, ISceneShape shape)
        {
            _collection = collection;
            _shape = shape;
        }

        public void Execute() => _collection.Add(_shape);
        public void Undo() => _collection.Remove(_shape);
    }
}
