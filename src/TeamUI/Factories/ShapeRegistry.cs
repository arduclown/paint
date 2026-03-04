using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using GraphicEditor.Common.Interfaces;
using GraphicEditor.Common.Models;
using GraphicEditor.TeamCore;
using GraphicEditor.ViewModels;

namespace GraphicEditor.ViewModels;

public record ShapeDescriptor(
    ToolType ToolType,
    string DisplayName,
    Key Hotkey,
    Func<Point, Point, IShape> CreateModel,
    Func<IShape, string, ShapeViewModel> CreateViewModel,
    Func<Point, Point, string?>? BuildPreview = null,
    Func<Point, Point, Point>? ShiftConstraint = null);

public static class ShapeRegistry
{
    private static readonly Dictionary<ToolType, ShapeDescriptor> _descriptors = [];
    private static readonly Dictionary<Key, ToolType> _hotkeyMap = [];
    static ShapeRegistry()
    {
        Register(new ShapeDescriptor(
            ToolType.Circle, "Круг", Key.C,
            (p1, p2) => ShapeFactory.CreateCircle(p1, p2),
            (model, name) => ShapeViewModelFactory.CreateCircle((TeamTools.Shapes.Circle)model, name),
            BuildCirclePreview,
            null));

        Register(new ShapeDescriptor(
            ToolType.Rectangle, "Прямоугольник", Key.R,
            (p1, p2) => ShapeFactory.CreateRectangle(p1, p2),
            (model, name) => ShapeViewModelFactory.CreateRectangle((TeamTools.Shapes.Rectangle)model, name),
            BuildRectPreview,
            SquareConstraint));

        Register(new ShapeDescriptor(
            ToolType.Triangle, "Треугольник", Key.T,
            (p1, p2) => ShapeFactory.CreateTriangle(p1, p2),
            (model, name) => ShapeViewModelFactory.CreateTriangle((TeamTools.Shapes.Triangle)model, name),
            BuildTrianglePreview,
            SquareConstraint));

        Register(new ShapeDescriptor(
            ToolType.Line, "Линия", Key.L,
            (p1, p2) => ShapeFactory.CreateLine(p1, p2),
            (model, name) => ShapeViewModelFactory.CreateLine((TeamTools.Shapes.Line)model, name),
            BuildLinePreview,
            LineAngleConstraint));
    }

    public static void Register(ShapeDescriptor descriptor)
    {
        _descriptors[descriptor.ToolType] = descriptor;
        _hotkeyMap[descriptor.Hotkey] = descriptor.ToolType;
    }

    public static ShapeDescriptor? GetByTool(ToolType tool) =>
        _descriptors.GetValueOrDefault(tool);

    public static ToolType? GetByHotkey(Key key) =>
        _hotkeyMap.TryGetValue(key, out var tool) ? tool : null;

    public static string GetDisplayName(ToolType tool) =>
        _descriptors.TryGetValue(tool, out var d) ? d.DisplayName : "Выбор";

    // Preview builders
    private static string? BuildCirclePreview(Point center, Point edge)
    {
        double dx = edge.X - center.X, dy = edge.Y - center.Y;
        double r = Math.Max(3, Math.Sqrt(dx * dx + dy * dy));
        return new TeamTools.Shapes.Circle(center, r).SerializedData;
    }

    private static string? BuildRectPreview(Point p1, Point p2)
    {
        double x1 = Math.Min(p1.X, p2.X), y1 = Math.Min(p1.Y, p2.Y);
        double x2 = Math.Max(p1.X, p2.X), y2 = Math.Max(p1.Y, p2.Y);
        return FormattableString.Invariant(
            $"M {x1:F2},{y1:F2} H {x2:F2} V {y2:F2} H {x1:F2} Z");
    }

    private static string? BuildTrianglePreview(Point p1, Point p2)
    {
        double x1 = Math.Min(p1.X, p2.X), x2 = Math.Max(p1.X, p2.X);
        double y1 = Math.Min(p1.Y, p2.Y), y2 = Math.Max(p1.Y, p2.Y);
        double cx = (x1 + x2) / 2.0;
        return FormattableString.Invariant(
            $"M {cx:F2},{y1:F2} L {x1:F2},{y2:F2} L {x2:F2},{y2:F2} Z");
    }

    private static string? BuildLinePreview(Point p1, Point p2) =>
        FormattableString.Invariant($"M {p1.X:F2},{p1.Y:F2} L {p2.X:F2},{p2.Y:F2}");

    // Shift constraints
    private static Point SquareConstraint(Point start, Point current)
    {
        double dx = current.X - start.X;
        double dy = current.Y - start.Y;
        double size = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return new Point(start.X + Math.Sign(dx) * size,
                         start.Y + Math.Sign(dy) * size);
    }

    private static Point LineAngleConstraint(Point start, Point current)
    {
        double dx = current.X - start.X;
        double dy = current.Y - start.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        double angle = Math.Atan2(dy, dx);
        double snapped = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);
        return new Point(start.X + len * Math.Cos(snapped),
                         start.Y + len * Math.Sin(snapped));
    }
}
