using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore.Commands
{
    public class ScaleShapeCommand : IEditorCommand
    {
        private readonly ISceneShape _shape;
        private readonly double _ratio;

        public ScaleShapeCommand(ISceneShape shape, double ratio)
        {
            _shape = shape;
            _ratio = ratio;
        }

        public void Execute() => _shape.Scale(_ratio);
        public void Undo() => _shape.Scale(1.0 / _ratio);
    }
}
