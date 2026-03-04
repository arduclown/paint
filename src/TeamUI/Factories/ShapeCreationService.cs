using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common.Models;

namespace GraphicEditor.ViewModels;

public class ShapeCreationService
{
    private readonly Dictionary<ToolType, int> _counters = [];

    public ShapeViewModel Create(
        ToolType tool, Point p1, Point p2,
        Color fill, Color stroke,
        string layerName, bool isVisible)
    {
        var descriptor = ShapeRegistry.GetByTool(tool)
            ?? throw new ArgumentException($"Unknown tool: {tool}");

        var model = descriptor.CreateModel(p1, p2);
        var name = NextName(tool);
        var vm = descriptor.CreateViewModel(model, name);

        vm.FillColor = fill;
        vm.StrokeColor = stroke;
        vm.LayerName = layerName;
        vm.IsVisible = isVisible;
        return vm;
    }

    public string NextName(ToolType tool)
    {
        if (!_counters.ContainsKey(tool)) _counters[tool] = 0;
        return $"{ShapeRegistry.GetDisplayName(tool)} {++_counters[tool]}";
    }

    public void ResetCounters()
    {
        foreach (var key in _counters.Keys.ToList())
            _counters[key] = 0;
    }
}
