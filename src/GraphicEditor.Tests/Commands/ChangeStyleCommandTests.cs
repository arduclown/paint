using Avalonia.Media;
using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class ChangeStyleCommandTests
{
    [Test]
    public void Execute_AppliesNewFillColor()
    {
        var shape = new FakeShape { FillColor = Colors.Red };
        var cmd = new ChangeStyleCommand(shape, Colors.Blue, Colors.Black);

        cmd.Execute();

        Assert.That(shape.FillColor, Is.EqualTo(Colors.Blue));
    }

    [Test]
    public void Execute_AppliesNewStrokeColor()
    {
        var shape = new FakeShape { StrokeColor = Colors.Black };
        var cmd = new ChangeStyleCommand(shape, Colors.Red, Colors.White);

        cmd.Execute();

        Assert.That(shape.StrokeColor, Is.EqualTo(Colors.White));
    }

    [Test]
    public void Undo_RestoresOriginalFillColor()
    {
        var shape = new FakeShape { FillColor = Colors.Red, StrokeColor = Colors.Black };
        var cmd = new ChangeStyleCommand(shape, Colors.Blue, Colors.White);
        cmd.Execute();

        cmd.Undo();

        Assert.That(shape.FillColor, Is.EqualTo(Colors.Red));
    }

    [Test]
    public void Undo_RestoresOriginalStrokeColor()
    {
        var shape = new FakeShape { FillColor = Colors.Red, StrokeColor = Colors.Black };
        var cmd = new ChangeStyleCommand(shape, Colors.Blue, Colors.White);
        cmd.Execute();

        cmd.Undo();

        Assert.That(shape.StrokeColor, Is.EqualTo(Colors.Black));
    }

    [Test]
    public void Constructor_CapturesColorsAtCreationTime_NotAtExecuteTime()
    {
        // Цвета меняются после создания команды, но до Execute —
        // Undo должен восстановить цвета на момент создания команды.
        var shape = new FakeShape { FillColor = Colors.Red, StrokeColor = Colors.Black };
        var cmd = new ChangeStyleCommand(shape, Colors.Blue, Colors.White);

        shape.FillColor = Colors.Green;    // меняем ДО Execute
        shape.StrokeColor = Colors.Yellow;

        cmd.Execute();
        cmd.Undo();

        Assert.That(shape.FillColor, Is.EqualTo(Colors.Red));
        Assert.That(shape.StrokeColor, Is.EqualTo(Colors.Black));
    }

    [Test]
    public void ExecuteThenUndo_BothColorsRestored()
    {
        var shape = new FakeShape { FillColor = Colors.Coral, StrokeColor = Colors.Teal };
        var cmd = new ChangeStyleCommand(shape, Colors.Gold, Colors.Purple);

        cmd.Execute();
        cmd.Undo();

        Assert.That(shape.FillColor, Is.EqualTo(Colors.Coral));
        Assert.That(shape.StrokeColor, Is.EqualTo(Colors.Teal));
    }

    [Test]
    public void Execute_WithSameColors_DoesNotThrow()
    {
        var shape = new FakeShape { FillColor = Colors.Red, StrokeColor = Colors.Black };
        var cmd = new ChangeStyleCommand(shape, Colors.Red, Colors.Black);

        Assert.DoesNotThrow(() => cmd.Execute());
    }
}
