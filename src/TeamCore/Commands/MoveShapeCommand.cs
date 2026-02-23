using Avalonia;
using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore.Commands
{
    public class MoveShapeCommand : IEditorCommand
    {
        private readonly ISceneShape _shape;
        private readonly Point _delta;

        public MoveShapeCommand(ISceneShape shape, Point delta)
        {
            _shape = shape;
            _delta = delta;
        }

        public void Execute() => _shape.Move(_delta);
        public void Undo() => _shape.Move(new Point(-_delta.X, -_delta.Y));
    }
}
