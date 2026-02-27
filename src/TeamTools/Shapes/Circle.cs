using System;
using Avalonia;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamTools.Shapes;

/// <summary>Круг — единственная фигура без полигонального представления.</summary>
public class Circle(Point center, double radius) : IShape
{
    private Point _center = radius > 0
        ? center
        : throw new ArgumentOutOfRangeException(nameof(radius));

    private double _radius = radius;

    public Point Center => _center;
    public double Radius => _radius;

    public void Move(Point offset) =>
        _center = new Point(_center.X + offset.X, _center.Y + offset.Y);

    public void Scale(double ratio)
    {
        if (ratio == 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        _radius *= ratio;
    }

    // У круга поворот и отражение не меняют геометрию
    public void Rotate(double angle) { }
    public void MirrorX() { }
    public void MirrorY() { }

    public string SerializedData => FormattableString.Invariant(
        $"M {_center.X - _radius:F2},{_center.Y:F2} A {_radius:F2},{_radius:F2},0,1,0,{_center.X + _radius:F2},{_center.Y:F2} A {_radius:F2},{_radius:F2},0,1,0,{_center.X - _radius:F2},{_center.Y:F2} Z");
}
