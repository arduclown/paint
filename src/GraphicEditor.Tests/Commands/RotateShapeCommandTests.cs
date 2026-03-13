using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class RotateShapeCommandTests
{
    [Test]
    public void Execute_RotatesShapeByAngle()
    {
        var shape = new FakeShape();
        var cmd = new RotateShapeCommand(shape, 45.0);

        cmd.Execute();

        Assert.That(shape.Angle, Is.EqualTo(45.0));
    }

    [Test]
    public void Execute_WithNegativeAngle_RotatesBackward()
    {
        var shape = new FakeShape();
        var cmd = new RotateShapeCommand(shape, -90.0);

        cmd.Execute();

        Assert.That(shape.Angle, Is.EqualTo(-90.0));
    }

    [Test]
    public void Undo_RotatesShapeByNegativeAngle()
    {
        var shape = new FakeShape();
        var cmd = new RotateShapeCommand(shape, 45.0);
        cmd.Execute();

        cmd.Undo();

        Assert.That(shape.Angle, Is.EqualTo(0.0));
    }

    [Test]
    public void ExecuteThenUndo_NetRotationIsZero()
    {
        var shape = new FakeShape();
        var cmd = new RotateShapeCommand(shape, 90.0);

        cmd.Execute();
        cmd.Undo();

        Assert.That(shape.Angle, Is.EqualTo(0.0));
    }

    [Test]
    public void Execute_CalledTwice_AccumulatesAngles()
    {
        var shape = new FakeShape();
        var cmd = new RotateShapeCommand(shape, 30.0);

        cmd.Execute();
        cmd.Execute();

        Assert.That(shape.Angle, Is.EqualTo(60.0));
    }

    [Test]
    public void Undo_WithZeroAngle_DoesNotChangeAngle()
    {
        var shape = new FakeShape();
        var cmd = new RotateShapeCommand(shape, 0.0);
        cmd.Execute();

        cmd.Undo();

        Assert.That(shape.Angle, Is.EqualTo(0.0));
    }
}
