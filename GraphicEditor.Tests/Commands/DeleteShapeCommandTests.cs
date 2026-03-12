using GraphicEditor.TeamCore.Commands;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Commands;

[TestFixture]
public class DeleteShapeCommandTests
{
    [Test]
    public void Execute_RemovesShapeFromCollection()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        collection.Add(shape);
        var cmd = new DeleteShapeCommand(collection, shape);

        cmd.Execute();

        Assert.That(collection.Count, Is.EqualTo(0));
    }

    [Test]
    public void Execute_WhenShapeNotInCollection_DoesNothing()
    {
        var collection = new FakeCollection();
        var other = new FakeShape();
        collection.Add(other);
        var cmd = new DeleteShapeCommand(collection, new FakeShape());

        cmd.Execute();

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection[0], Is.SameAs(other));
    }

    [Test]
    public void Execute_DoesNotAffectOtherShapes()
    {
        var collection = new FakeCollection();
        var a = new FakeShape();
        var b = new FakeShape();
        collection.Add(a);
        collection.Add(b);
        var cmd = new DeleteShapeCommand(collection, a);

        cmd.Execute();

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection[0], Is.SameAs(b));
    }

    [Test]
    public void Undo_ReInsertsShapeAtOriginalIndex()
    {
        var collection = new FakeCollection();
        var a = new FakeShape();
        var b = new FakeShape();
        var c = new FakeShape();
        collection.Add(a);
        collection.Add(b); // index 1
        collection.Add(c);
        var cmd = new DeleteShapeCommand(collection, b);
        cmd.Execute();

        cmd.Undo();

        Assert.That(collection.Count, Is.EqualTo(3));
        Assert.That(collection[1], Is.SameAs(b));
    }

    [Test]
    public void Undo_BeforeExecute_DoesNothing()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        collection.Add(shape);
        var cmd = new DeleteShapeCommand(collection, shape);

        cmd.Undo(); // _index is -1, должен быть no-op

        Assert.That(collection.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExecuteThenUndo_CollectionRestoredToOriginalState()
    {
        var collection = new FakeCollection();
        var shape = new FakeShape();
        collection.Add(shape);
        var cmd = new DeleteShapeCommand(collection, shape);

        cmd.Execute();
        cmd.Undo();

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection[0], Is.SameAs(shape));
    }

    [Test]
    public void ExecuteThenUndo_PreservesRelativeOrder()
    {
        var collection = new FakeCollection();
        var a = new FakeShape();
        var b = new FakeShape();
        var c = new FakeShape();
        collection.Add(a);
        collection.Add(b);
        collection.Add(c);
        var cmd = new DeleteShapeCommand(collection, b);

        cmd.Execute();
        cmd.Undo();

        Assert.That(collection[0], Is.SameAs(a));
        Assert.That(collection[1], Is.SameAs(b));
        Assert.That(collection[2], Is.SameAs(c));
    }
}
