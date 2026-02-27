using System;
using System.Collections.Generic;
using System.Linq;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamCore;

/// <summary>Управление слоями: создание, удаление, видимость, активный слой.</summary>
public class LayerManager(IEnumerable<ISceneShape> shapes)
{
    private readonly List<ILayer> _layers = [];
    private ILayer? _activeLayer;

    public event Action<ILayer>? LayerCreated;

    public IReadOnlyList<ILayer> Layers => _layers;
    public ILayer? ActiveLayer => _activeLayer;

    public void Register(ILayer layer) => _layers.Add(layer);

    public bool Unregister(ILayer layer)
    {
        if (_layers.Count <= 1) return false;
        _layers.Remove(layer);
        return true;
    }

    public void SetActive(ILayer layer)
    {
        if (_activeLayer is not null) _activeLayer.IsActive = false;
        _activeLayer = layer;
        if (_activeLayer is not null) _activeLayer.IsActive = true;
    }

    public ILayer? FindLayer(string name) =>
        _layers.FirstOrDefault(l => l.Name == name);

    public ILayer GetOrCreate(string name, Func<string, ILayer> factory)
    {
        var existing = FindLayer(name);
        if (existing is not null) return existing;

        var newLayer = factory(name);
        _layers.Add(newLayer);
        LayerCreated?.Invoke(newLayer);
        return newLayer;
    }

    public void ApplyVisibility(ILayer layer)
    {
        foreach (var shape in shapes)
            if (shape.LayerName == layer.Name)
                shape.IsVisible = layer.IsVisible;
    }

    public void MoveShapeToLayer(ISceneShape shape, ILayer target)
    {
        shape.LayerName = target.Name;
        shape.IsVisible = target.IsVisible;
    }
}
