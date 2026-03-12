using System;
using Avalonia;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamTools.Shapes;

public class Circle : IShape
{
    private Point _center;
    private double _radiusX;
    private double _radiusY;

    public Circle(Point center, double radius)
    {
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        _center = center;
        _radiusX = radius;
        _radiusY = radius;
    }

    public Circle(Point center, double radiusX, double radiusY)
    {
        if (radiusX <= 0) throw new ArgumentOutOfRangeException(nameof(radiusX));
        if (radiusY <= 0) throw new ArgumentOutOfRangeException(nameof(radiusY));
        _center = center;
        _radiusX = radiusX;
        _radiusY = radiusY;
    }

    public Point Center => _center;
    public double RadiusX => _radiusX;
    public double RadiusY => _radiusY;

    public void Move(Point offset) =>
        _center = new Point(_center.X + offset.X, _center.Y + offset.Y);

    public void Scale(double ratio)
    {
        if (ratio == 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        _radiusX *= ratio;
        _radiusY *= ratio;
    }

    public void ScaleXY(double sx, double sy)
    {
        if (sx <= 0) throw new ArgumentOutOfRangeException(nameof(sx));
        if (sy <= 0) throw new ArgumentOutOfRangeException(nameof(sy));
        _radiusX *= sx;
        _radiusY *= sy;
    }

    public void Rotate(double angle) { }
    public void MirrorX() { }
    public void MirrorY() { }

    public string SerializedData => FormattableString.Invariant(
        $"M {_center.X - _radiusX:F2},{_center.Y:F2} A {_radiusX:F2},{_radiusY:F2},0,1,0,{_center.X + _radiusX:F2},{_center.Y:F2} A {_radiusX:F2},{_radiusY:F2},0,1,0,{_center.X - _radiusX:F2},{_center.Y:F2} Z");
}
