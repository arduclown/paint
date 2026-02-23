using Avalonia.Media;
using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore.Commands
{
    public class ChangeStyleCommand : IEditorCommand
    {
        private readonly ISceneShape _shape;
        private readonly Color _newFill;
        private readonly Color _oldFill;
        private readonly Color _newStroke;
        private readonly Color _oldStroke;

        public ChangeStyleCommand(ISceneShape shape, Color newFill, Color newStroke)
        {
            _shape = shape;
            _newFill = newFill;
            _newStroke = newStroke;
            _oldFill = shape.FillColor;
            _oldStroke = shape.StrokeColor;
        }

        public void Execute()
        {
            _shape.FillColor = _newFill;
            _shape.StrokeColor = _newStroke;
        }

        public void Undo()
        {
            _shape.FillColor = _oldFill;
            _shape.StrokeColor = _oldStroke;
        }
    }
}
