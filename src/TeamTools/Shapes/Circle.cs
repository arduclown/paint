using System;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes
{
    public class Circle : IShape
    {
        private Point _center;
        private double _radius;

        public Point Center => _center;
        public double Radius => _radius;

        public Circle(Point center, double radius)
        {
            if (radius <= 0)
                throw new ArgumentOutOfRangeException(nameof(radius), "Радиус должен быть положительным");
            _center = center;
            _radius = radius;
        }

        public void Move(Point offset)
        {
            _center = new Point(_center.X + offset.X, _center.Y + offset.Y);
        }

        public void Scale(double ratio)
        {
            if (ratio == 0)
                throw new ArgumentOutOfRangeException(nameof(ratio), "Масштаб не должен быть равен нулю");
            _radius *= ratio;
        }

        public void Rotate(double angle) { }
        public void MirrorX() { }
        public void MirrorY() { }

        public string SerializedData => FormattableString.Invariant(
            $"M {_center.X - _radius:F2},{_center.Y:F2} A {_radius:F2},{_radius:F2},0,1,0,{_center.X + _radius:F2},{_center.Y:F2} A {_radius:F2},{_radius:F2},0,1,0,{_center.X - _radius:F2},{_center.Y:F2} Z");
    }
}
