using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore.Commands
{
    public class RotateShapeCommand : IEditorCommand
    {
        private readonly ISceneShape _shape;
        private readonly double _angle;

        public RotateShapeCommand(ISceneShape shape, double angle)
        {
            _shape = shape;
            _angle = angle;
        }

        public void Execute() => _shape.Rotate(_angle);
        public void Undo() => _shape.Rotate(-_angle);
    }
}
