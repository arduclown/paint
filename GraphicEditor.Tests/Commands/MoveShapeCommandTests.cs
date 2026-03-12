using Avalonia;
using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class MoveShapeCommandTests
{
    [Test]
    public void Execute_MovesShapeByDelta()
    {
        var shape = new FakeShape();
        var cmd = new MoveShapeCommand(shape, new Point(10, 20));

        cmd.Execute();

        Assert.That(shape.X, Is.EqualTo(10));
        Assert.That(shape.Y, Is.EqualTo(20));
    }

    [Test]
    public void Execute_WithNegativeDelta_MovesShapeBackward()
    {
        var shape = new FakeShape();
        var cmd = new MoveShapeCommand(shape, new Point(-5, -15));

        cmd.Execute();

        Assert.That(shape.X, Is.EqualTo(-5));
        Assert.That(shape.Y, Is.EqualTo(-15));
    }

    [Test]
    public void Undo_MovesShapeByNegativeDelta()
    {
        var shape = new FakeShape();
        var cmd = new MoveShapeCommand(shape, new Point(10, 20));
        cmd.Execute();

        cmd.Undo();

        Assert.That(shape.X, Is.EqualTo(0));
        Assert.That(shape.Y, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteThenUndo_ShapeReturnedToOriginalPosition()
    {
        var shape = new FakeShape();
        var cmd = new MoveShapeCommand(shape, new Point(50, -30));

        cmd.Execute();
        cmd.Undo();

        Assert.That(shape.X, Is.EqualTo(0));
        Assert.That(shape.Y, Is.EqualTo(0));
    }

    [Test]
    public void Execute_CalledTwice_AccumulatesDeltas()
    {
        var shape = new FakeShape();
        var cmd = new MoveShapeCommand(shape, new Point(10, 0));

        cmd.Execute();
        cmd.Execute();

        Assert.That(shape.X, Is.EqualTo(20));
    }

    [Test]
    public void Undo_CalledTwice_AccumulatesNegativeDeltas()
    {
        var shape = new FakeShape();
        var cmd = new MoveShapeCommand(shape, new Point(10, 0));
        cmd.Execute();
        cmd.Execute();

        cmd.Undo();
        cmd.Undo();

        Assert.That(shape.X, Is.EqualTo(0));
    }
}
