using Avalonia;
using Avalonia.Media;
using GraphicEditor.TeamCore;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Core;

[TestFixture]
public class SceneManagerTests
{
    // Начальное состояние

    [Test]
    public void CanUndo_FalseWhenNoCommandsExecuted()
    {
        var manager = new SceneManager(new FakeCollection());
        Assert.That(manager.CanUndo, Is.False);
    }

    [Test]
    public void CanRedo_FalseWhenNoCommandsExecuted()
    {
        var manager = new SceneManager(new FakeCollection());
        Assert.That(manager.CanRedo, Is.False);
    }

    // Add
    [Test]
    public void Add_AddsShapeToCollection()
    {
        var collection = new FakeCollection();
        var manager = new SceneManager(collection);
        var shape = new FakeShape();

        manager.Add(shape);

        Assert.That(collection.Shapes, Has.Count.EqualTo(1));
        Assert.That(collection.Shapes[0], Is.SameAs(shape));
    }

    [Test]
    public void Add_EnablesUndo()
    {
        var manager = new SceneManager(new FakeCollection());

        manager.Add(new FakeShape());

        Assert.That(manager.CanUndo, Is.True);
    }

    // Delete 

    [Test]
    public void Delete_RemovesShapeFromCollection()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        collection.Add(shape);
        var manager = new SceneManager(collection);

        manager.Delete(shape);

        Assert.That(collection.Shapes, Is.Empty);
    }

    [Test]
    public void Delete_EnablesUndo()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        collection.Add(shape);
        var manager = new SceneManager(collection);

        manager.Delete(shape);

        Assert.That(manager.CanUndo, Is.True);
    }

    // Move

    [Test]
    public void Move_MovesShapeByDelta()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());

        manager.Move(shape, new Point(10, 5));

        Assert.That(shape.X, Is.EqualTo(10));
        Assert.That(shape.Y, Is.EqualTo(5));
    }

    [Test]
    public void Move_EnablesUndo()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());

        manager.Move(shape, new Point(1, 0));

        Assert.That(manager.CanUndo, Is.True);
    }

    // Rotate

    [Test]
    public void Rotate_RotatesShapeByAngle()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());

        manager.Rotate(shape, 45.0);

        Assert.That(shape.Angle, Is.EqualTo(45.0));
    }

    [Test]
    public void Rotate_EnablesUndo()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());

        manager.Rotate(shape, 30.0);

        Assert.That(manager.CanUndo, Is.True);
    }

    // ChangeStyle

    [Test]
    public void ChangeStyle_UpdatesShapeColors()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());

        manager.ChangeStyle(shape, Colors.Blue, Colors.Green);

        Assert.That(shape.FillColor, Is.EqualTo(Colors.Blue));
        Assert.That(shape.StrokeColor, Is.EqualTo(Colors.Green));
    }

    [Test]
    public void ChangeStyle_EnablesUndo()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());

        manager.ChangeStyle(shape, Colors.Blue, Colors.Green);

        Assert.That(manager.CanUndo, Is.True);
    }

    // Undo

    [Test]
    public void Undo_RevertsAdd()
    {
        var collection = new FakeCollection();
        var manager = new SceneManager(collection);
        var shape = new FakeShape();
        manager.Add(shape);

        manager.Undo();

        Assert.That(collection.Shapes, Is.Empty);
    }

    [Test]
    public void Undo_RevertsMove()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());
        manager.Move(shape, new Point(10, 0));

        manager.Undo();

        Assert.That(shape.X, Is.EqualTo(0));
    }

    [Test]
    public void Undo_EnablesRedo()
    {
        var manager = new SceneManager(new FakeCollection());
        manager.Move(new FakeShape(), new Point(1, 0));

        manager.Undo();

        Assert.That(manager.CanRedo, Is.True);
    }

    [Test]
    public void Undo_WhenEmpty_DoesNotThrow()
    {
        var manager = new SceneManager(new FakeCollection());
        Assert.DoesNotThrow(() => manager.Undo());
    }

    // Redo

    [Test]
    public void Redo_ReappliesMove()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());
        manager.Move(shape, new Point(10, 0));
        manager.Undo();

        manager.Redo();

        Assert.That(shape.X, Is.EqualTo(10));
    }

    [Test]
    public void Redo_WhenEmpty_DoesNotThrow()
    {
        var manager = new SceneManager(new FakeCollection());
        Assert.DoesNotThrow(() => manager.Redo());
    }

    // Сложные сценарии

    [Test]
    public void NewCommandAfterUndo_ClearsRedo()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());
        manager.Move(shape, new Point(10, 0));
        manager.Undo();

        manager.Move(shape, new Point(5, 0));

        Assert.That(manager.CanRedo, Is.False);
    }

    [Test]
    public void MultipleCommands_UndoRedoWorkInOrder()
    {
        var shape = new FakeShape();
        var manager = new SceneManager(new FakeCollection());
        manager.Move(shape, new Point(10, 0));
        manager.Move(shape, new Point(5, 0));
        // shape.X == 15

        manager.Undo(); // x = 10
        Assert.That(shape.X, Is.EqualTo(10));

        manager.Undo(); // x = 0
        Assert.That(shape.X, Is.EqualTo(0));

        manager.Redo(); // x = 10
        Assert.That(shape.X, Is.EqualTo(10));
    }
}
