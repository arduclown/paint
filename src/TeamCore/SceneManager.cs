using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common.Interfaces;
using GraphicEditor.TeamCore.Commands;

namespace GraphicEditor.TeamCore;

public class SceneManager(ISceneCollection shapes)
{
    private readonly CommandManager _commandManager = new();

    public bool CanUndo => _commandManager.CanUndo;
    public bool CanRedo => _commandManager.CanRedo;

    public void Add(ISceneShape shape) =>
        _commandManager.ExecuteCommand(new AddShapeCommand(shapes, shape));

    public void Delete(ISceneShape shape) =>
        _commandManager.ExecuteCommand(new DeleteShapeCommand(shapes, shape));

    public void Move(ISceneShape shape, Point delta) =>
        _commandManager.ExecuteCommand(new MoveShapeCommand(shape, delta));

    public void Rotate(ISceneShape shape, double angle) =>
        _commandManager.ExecuteCommand(new RotateShapeCommand(shape, angle));

    public void ChangeStyle(ISceneShape shape, Color fill, Color stroke) =>
        _commandManager.ExecuteCommand(new ChangeStyleCommand(shape, fill, stroke));

    public void Undo() => _commandManager.Undo();
    public void Redo() => _commandManager.Redo();
}
