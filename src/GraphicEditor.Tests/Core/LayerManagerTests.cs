using GraphicEditor.Common.Interfaces;
using GraphicEditor.TeamCore;
using GraphicEditor.Tests.Fakes;

namespace GraphicEditor.Tests.Core;

[TestFixture]
public class LayerManagerTests
{
    // Register / Layers

    [Test]
    public void Register_AddsLayerToList()
    {
        var manager = new LayerManager([]);
        var layer = new FakeLayer("A");

        manager.Register(layer);

        Assert.That(manager.Layers, Has.Count.EqualTo(1));
        Assert.That(manager.Layers[0], Is.SameAs(layer));
    }

    [Test]
    public void Register_MultipleLayersAppendedInOrder()
    {
        var manager = new LayerManager([]);
        var a = new FakeLayer("A");
        var b = new FakeLayer("B");

        manager.Register(a);
        manager.Register(b);

        Assert.That(manager.Layers, Is.EqualTo(new[] { a, b }));
    }

    // Unregister
    [Test]
    public void Unregister_WithTwoLayers_RemovesAndReturnsTrue()
    {
        var manager = new LayerManager([]);
        var a = new FakeLayer("A");
        var b = new FakeLayer("B");
        manager.Register(a);
        manager.Register(b);

        bool result = manager.Unregister(a);

        Assert.That(result, Is.True);
        Assert.That(manager.Layers, Has.Count.EqualTo(1));
        Assert.That(manager.Layers[0], Is.SameAs(b));
    }

    [Test]
    public void Unregister_WithOneLayer_ReturnsFalseAndKeepsLayer()
    {
        var manager = new LayerManager([]);
        var a = new FakeLayer("A");
        manager.Register(a);

        bool result = manager.Unregister(a);

        Assert.That(result, Is.False);
        Assert.That(manager.Layers, Has.Count.EqualTo(1));
    }

    // SetActive

    [Test]
    public void SetActive_SetsIsActiveOnTargetLayer()
    {
        var manager = new LayerManager([]);
        var layer = new FakeLayer("A");

        manager.SetActive(layer);

        Assert.That(layer.IsActive, Is.True);
        Assert.That(manager.ActiveLayer, Is.SameAs(layer));
    }

    [Test]
    public void SetActive_DeactivatesPreviousActiveLayer()
    {
        var manager = new LayerManager([]);
        var first = new FakeLayer("A");
        var second = new FakeLayer("B");

        manager.SetActive(first);
        manager.SetActive(second);

        Assert.That(first.IsActive, Is.False);
        Assert.That(second.IsActive, Is.True);
    }

    [Test]
    public void ActiveLayer_IsNullInitially()
    {
        var manager = new LayerManager([]);
        Assert.That(manager.ActiveLayer, Is.Null);
    }

    // FindLayer

    [Test]
    public void FindLayer_ReturnsLayerByName()
    {
        var manager = new LayerManager([]);
        var layer = new FakeLayer("Background");
        manager.Register(layer);

        var found = manager.FindLayer("Background");

        Assert.That(found, Is.SameAs(layer));
    }

    [Test]
    public void FindLayer_ReturnsNullIfNotFound()
    {
        var manager = new LayerManager([]);

        var found = manager.FindLayer("Missing");

        Assert.That(found, Is.Null);
    }

    // GetOrCreate

    [Test]
    public void GetOrCreate_ReturnsExistingLayerWithoutCreating()
    {
        var manager = new LayerManager([]);
        var existing = new FakeLayer("Layer1");
        manager.Register(existing);

        var result = manager.GetOrCreate("Layer1", name => new FakeLayer(name));

        Assert.That(result, Is.SameAs(existing));
        Assert.That(manager.Layers, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetOrCreate_CreatesAndRegistersNewLayerWhenMissing()
    {
        var manager = new LayerManager([]);

        var result = manager.GetOrCreate("NewLayer", name => new FakeLayer(name));

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("NewLayer"));
        Assert.That(manager.Layers, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetOrCreate_FiresLayerCreatedEventForNewLayer()
    {
        var manager = new LayerManager([]);
        ILayer? fired = null;
        manager.LayerCreated += l => fired = l;

        var result = manager.GetOrCreate("New", name => new FakeLayer(name));

        Assert.That(fired, Is.SameAs(result));
    }

    [Test]
    public void GetOrCreate_DoesNotFireLayerCreatedForExistingLayer()
    {
        var manager = new LayerManager([]);
        manager.Register(new FakeLayer("Existing"));
        bool eventFired = false;
        manager.LayerCreated += _ => eventFired = true;

        manager.GetOrCreate("Existing", name => new FakeLayer(name));

        Assert.That(eventFired, Is.False);
    }

    // ApplyVisibility

    [Test]
    public void ApplyVisibility_HidesShapesOnLayer()
    {
        var shape = new FakeShape { LayerName = "Layer1", IsVisible = true };
        var manager = new LayerManager([shape]);
        var layer = new FakeLayer("Layer1") { IsVisible = false };

        manager.ApplyVisibility(layer);

        Assert.That(shape.IsVisible, Is.False);
    }

    [Test]
    public void ApplyVisibility_ShowsShapesOnLayer()
    {
        var shape = new FakeShape { LayerName = "Layer1", IsVisible = false };
        var manager = new LayerManager([shape]);
        var layer = new FakeLayer("Layer1") { IsVisible = true };

        manager.ApplyVisibility(layer);

        Assert.That(shape.IsVisible, Is.True);
    }

    [Test]
    public void ApplyVisibility_DoesNotAffectShapesOnOtherLayer()
    {
        var shape = new FakeShape { LayerName = "Other", IsVisible = true };
        var manager = new LayerManager([shape]);
        var layer = new FakeLayer("Layer1") { IsVisible = false };

        manager.ApplyVisibility(layer);

        Assert.That(shape.IsVisible, Is.True);
    }

    [Test]
    public void ApplyVisibility_AffectsAllShapesOnLayer()
    {
        var s1 = new FakeShape { LayerName = "L", IsVisible = true };
        var s2 = new FakeShape { LayerName = "L", IsVisible = true };
        var s3 = new FakeShape { LayerName = "Other", IsVisible = true };
        var manager = new LayerManager([s1, s2, s3]);
        var layer = new FakeLayer("L") { IsVisible = false };

        manager.ApplyVisibility(layer);

        Assert.That(s1.IsVisible, Is.False);
        Assert.That(s2.IsVisible, Is.False);
        Assert.That(s3.IsVisible, Is.True);
    }

    // MoveShapeToLayer

    [Test]
    public void MoveShapeToLayer_UpdatesShapeLayerName()
    {
        var shape = new FakeShape { LayerName = "Old" };
        var manager = new LayerManager([shape]);
        var target = new FakeLayer("New");

        manager.MoveShapeToLayer(shape, target);

        Assert.That(shape.LayerName, Is.EqualTo("New"));
    }

    [Test]
    public void MoveShapeToLayer_SetsShapeVisibilityToTargetLayerVisibility()
    {
        var shape = new FakeShape { IsVisible = true };
        var manager = new LayerManager([shape]);
        var target = new FakeLayer("New") { IsVisible = false };

        manager.MoveShapeToLayer(shape, target);

        Assert.That(shape.IsVisible, Is.False);
    }
}
