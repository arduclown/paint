using Avalonia;
using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class CommandManagerTests
{
    // Начальное состояние

    [Test]
    public void CanUndo_FalseWhenEmpty()
    {
        var manager = new CommandManager();
        Assert.That(manager.CanUndo, Is.False);
    }

    [Test]
    public void CanRedo_FalseWhenEmpty()
    {
        var manager = new CommandManager();
        Assert.That(manager.CanRedo, Is.False);
    }

    // ExecuteCommand

    [Test]
    public void ExecuteCommand_CallsExecuteOnCommand()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();

        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(5, 0)));

        Assert.That(shape.X, Is.EqualTo(5));
    }

    [Test]
    public void ExecuteCommand_EnablesUndo()
    {
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(new FakeShape(), new Point(1, 0)));

        Assert.That(manager.CanUndo, Is.True);
    }

    [Test]
    public void ExecuteCommand_DoesNotEnableRedo()
    {
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(new FakeShape(), new Point(1, 0)));

        Assert.That(manager.CanRedo, Is.False);
    }

    [Test]
    public void ExecuteCommand_ClearsRedoStack()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(1, 0)));
        manager.Undo();
        Assert.That(manager.CanRedo, Is.True);

        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(2, 0)));

        Assert.That(manager.CanRedo, Is.False);
    }

    // Undo

    [Test]
    public void Undo_CallsUndoOnCommand()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));

        manager.Undo();

        Assert.That(shape.X, Is.EqualTo(0));
    }

    [Test]
    public void Undo_EnablesRedo()
    {
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(new FakeShape(), new Point(1, 0)));

        manager.Undo();

        Assert.That(manager.CanRedo, Is.True);
    }

    [Test]
    public void Undo_RemovesCommandFromUndoStack()
    {
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(new FakeShape(), new Point(1, 0)));

        manager.Undo();

        Assert.That(manager.CanUndo, Is.False);
    }

    [Test]
    public void Undo_WhenEmpty_DoesNotThrow()
    {
        var manager = new CommandManager();
        Assert.DoesNotThrow(() => manager.Undo());
    }

    [Test]
    public void Undo_WhenEmpty_DoesNotEnableRedo()
    {
        var manager = new CommandManager();
        manager.Undo();
        Assert.That(manager.CanRedo, Is.False);
    }

    // Redo

    [Test]
    public void Redo_CallsExecuteOnCommand()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));
        manager.Undo();

        manager.Redo();

        Assert.That(shape.X, Is.EqualTo(10));
    }

    [Test]
    public void Redo_EnablesUndo()
    {
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(new FakeShape(), new Point(1, 0)));
        manager.Undo();

        manager.Redo();

        Assert.That(manager.CanUndo, Is.True);
    }

    [Test]
    public void Redo_RemovesCommandFromRedoStack()
    {
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(new FakeShape(), new Point(1, 0)));
        manager.Undo();

        manager.Redo();

        Assert.That(manager.CanRedo, Is.False);
    }

    [Test]
    public void Redo_WhenEmpty_DoesNotThrow()
    {
        var manager = new CommandManager();
        Assert.DoesNotThrow(() => manager.Redo());
    }

    // Сложные сценарии

    [Test]
    public void MultipleUndo_WorksSequentially()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(5, 0)));
        // shape.X == 15

        manager.Undo(); // откат второго: x = 10
        Assert.That(shape.X, Is.EqualTo(10));

        manager.Undo(); // откат первого: x = 0
        Assert.That(shape.X, Is.EqualTo(0));
    }

    [Test]
    public void MultipleRedo_WorksSequentially()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(5, 0)));
        manager.Undo();
        manager.Undo();

        manager.Redo(); // повтор первого: x = 10
        Assert.That(shape.X, Is.EqualTo(10));

        manager.Redo(); // повтор второго: x = 15
        Assert.That(shape.X, Is.EqualTo(15));
    }

    [Test]
    public void UndoRedoUndo_SequenceWorksCorrectly()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));

        manager.Undo();  // x = 0
        manager.Redo();  // x = 10
        manager.Undo();  // x = 0

        Assert.That(shape.X, Is.EqualTo(0));
    }

    [Test]
    public void NewCommandAfterUndo_ClearsForwardHistory()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(5, 0)));
        manager.Undo(); // откатить второй

        // Новая команда — история «вперёд» должна стереться
        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(20, 0)));

        Assert.That(manager.CanRedo, Is.False);
        Assert.That(shape.X, Is.EqualTo(30)); // 10 + 20
    }

    [Test]
    public void DifferentCommandTypes_InterleaveCorrectly()
    {
        var shape = new FakeShape();
        var manager = new CommandManager();

        manager.ExecuteCommand(new MoveShapeCommand(shape, new Point(10, 0)));
        manager.ExecuteCommand(new ScaleShapeCommand(shape, 2.0));
        manager.ExecuteCommand(new RotateShapeCommand(shape, 90.0));

        manager.Undo(); // отменить поворот
        Assert.That(shape.Angle, Is.EqualTo(0.0));

        manager.Undo(); // отменить масштаб
        Assert.That(shape.ScaleFactor, Is.EqualTo(1.0).Within(1e-10));

        manager.Undo(); // отменить перемещение
        Assert.That(shape.X, Is.EqualTo(0));
    }
}
