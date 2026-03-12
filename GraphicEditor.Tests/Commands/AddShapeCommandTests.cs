using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class AddShapeCommandTests
{
    [Test]
    public void Execute_AddsShapeToCollection()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        var cmd = new AddShapeCommand(collection, shape);

        cmd.Execute();

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection[0], Is.SameAs(shape));
    }

    [Test]
    public void Execute_CalledTwice_AddsDuplicates()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        var cmd = new AddShapeCommand(collection, shape);

        cmd.Execute();
        cmd.Execute();

        Assert.That(collection.Count, Is.EqualTo(2));
    }

    [Test]
    public void Undo_RemovesShapeFromCollection()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        var cmd = new AddShapeCommand(collection, shape);
        cmd.Execute();

        cmd.Undo();

        Assert.That(collection.Count, Is.EqualTo(0));
    }

    [Test]
    public void Undo_DoesNotAffectOtherShapes()
    {
        var collection = new FakeCollection();
        var other = new FakeShape();
        collection.Add(other);
        var shape = new FakeShape();
        var cmd = new AddShapeCommand(collection, shape);
        cmd.Execute();

        cmd.Undo();

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection[0], Is.SameAs(other));
    }

    [Test]
    public void ExecuteThenUndo_CollectionReturnedToOriginalState()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        var cmd = new AddShapeCommand(collection, shape);

        cmd.Execute();
        cmd.Undo();

        Assert.That(collection.Count, Is.EqualTo(0));
    }
}
