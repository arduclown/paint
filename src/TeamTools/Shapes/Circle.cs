using System;
using Avalonia;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamTools.Shapes;

public class Circle(Point center, double radius) : IShape
{
    private Point _center = radius > 0
        ? center
        : throw new ArgumentOutOfRangeException(nameof(radius));

    private double _radiusX = radius;
    private double _radiusY = radius;

    public Point Center => _center;
    public double Radius => _radiusX;
    public double RadiusX => _radiusX;
    public double RadiusY => _radiusY;

    public Circle(Point center, double radiusX, double radiusY) : this(center, radiusX)
    {
        _radiusY = radiusY > 0 ? radiusY : throw new ArgumentOutOfRangeException(nameof(radiusY));
    }

    public void Move(Point offset) =>
        _center = new Point(_center.X + offset.X, _center.Y + offset.Y);

    public void Scale(double ratio)
    {
        if (ratio == 0) throw new ArgumentOutOfRangeException(nameof(ratio));
        _radiusX *= ratio;
        _radiusY *= ratio;
    }

    public void Scale(double ratioX, double ratioY)
    {
        if (ratioX == 0) throw new ArgumentOutOfRangeException(nameof(ratioX));
        if (ratioY == 0) throw new ArgumentOutOfRangeException(nameof(ratioY));
        _radiusX *= ratioX;
        _radiusY *= ratioY;
    }

    public void Rotate(double angle) { }
    public void MirrorX() { }
    public void MirrorY() { }

    public string SerializedData => FormattableString.Invariant(
        $"M {_center.X - _radiusX:F2},{_center.Y:F2} A {_radiusX:F2},{_radiusY:F2},0,1,0,{_center.X + _radiusX:F2},{_center.Y:F2} A {_radiusX:F2},{_radiusY:F2},0,1,0,{_center.X - _radiusX:F2},{_center.Y:F2} Z");
}
