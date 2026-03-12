using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class ScaleShapeCommandTests
{
    [Test]
    public void Execute_ScalesShapeByRatio()
    {
        var shape = new FakeShape();
        var cmd = new ScaleShapeCommand(shape, 2.0);

        cmd.Execute();

        Assert.That(shape.ScaleFactor, Is.EqualTo(2.0));
    }

    [Test]
    public void Execute_WithFractionalRatio_ScalesDown()
    {
        var shape = new FakeShape();
        var cmd = new ScaleShapeCommand(shape, 0.5);

        cmd.Execute();

        Assert.That(shape.ScaleFactor, Is.EqualTo(0.5));
    }

    [Test]
    public void Undo_ScalesShapeByInverseRatio()
    {
        var shape = new FakeShape();
        var cmd = new ScaleShapeCommand(shape, 2.0);
        cmd.Execute();

        cmd.Undo();

        Assert.That(shape.ScaleFactor, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void ExecuteThenUndo_ScaleFactorRestoredToOne()
    {
        var shape = new FakeShape();
        var cmd = new ScaleShapeCommand(shape, 3.0);

        cmd.Execute();
        cmd.Undo();

        Assert.That(shape.ScaleFactor, Is.EqualTo(1.0).Within(1e-10));
    }

    [Test]
    public void Execute_CalledTwice_AccumulatesScale()
    {
        var shape = new FakeShape();
        var cmd = new ScaleShapeCommand(shape, 2.0);

        cmd.Execute();
        cmd.Execute();

        Assert.That(shape.ScaleFactor, Is.EqualTo(4.0));
    }

    [Test]
    public void Undo_CalledTwice_FullyUndoesTwoExecutes()
    {
        var shape = new FakeShape();
        var cmd = new ScaleShapeCommand(shape, 2.0);
        cmd.Execute();
        cmd.Execute();

        cmd.Undo();
        cmd.Undo();

        Assert.That(shape.ScaleFactor, Is.EqualTo(1.0).Within(1e-10));
    }
}
