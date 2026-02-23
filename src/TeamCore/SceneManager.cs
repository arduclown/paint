using Avalonia;
using Avalonia.Media;
using GraphicEditor.TeamCore.Commands;
using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.TeamCore
{
    // Управляет операциями над фигурами сцены: добавление, удаление,
    // трансформации, изменение стиля, отмена/повтор действий.
    public class SceneManager
    {
        private readonly CommandManager _commandManager = new();
        private readonly ISceneCollection _shapes;

        public SceneManager(ISceneCollection shapes)
        {
            _shapes = shapes;
        }

        public bool CanUndo => _commandManager.CanUndo;
        public bool CanRedo => _commandManager.CanRedo;

        public void Add(ISceneShape shape) =>
            _commandManager.ExecuteCommand(new AddShapeCommand(_shapes, shape));

        public void Delete(ISceneShape shape) =>
            _commandManager.ExecuteCommand(new DeleteShapeCommand(_shapes, shape));

        public void Move(ISceneShape shape, Point delta) =>
            _commandManager.ExecuteCommand(new MoveShapeCommand(shape, delta));

        public void Rotate(ISceneShape shape, double angle) =>
            _commandManager.ExecuteCommand(new RotateShapeCommand(shape, angle));

        public void ChangeStyle(ISceneShape shape, Color fill, Color stroke) =>
            _commandManager.ExecuteCommand(new ChangeStyleCommand(shape, fill, stroke));

        public void Undo() => _commandManager.Undo();
        public void Redo() => _commandManager.Redo();
    }
}
