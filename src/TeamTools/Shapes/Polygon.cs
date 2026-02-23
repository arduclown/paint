using System;
using System.Linq;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes
{
    public abstract class Polygon : IShape
    {
        private Point[] _points;

        public Point[] Points => _points;

        protected Polygon(Point[] points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Length == 0)
                throw new ArgumentException("Коллекция точек не должна быть пустой", nameof(points));
            _points = points;
        }

        public abstract string SerializedData { get; }

        protected Point GetCenter()
        {
            var minX = _points.Min(p => p.X);
            var maxX = _points.Max(p => p.X);
            var minY = _points.Min(p => p.Y);
            var maxY = _points.Max(p => p.Y);
            return new Point((minX + maxX) / 2.0, (minY + maxY) / 2.0);
        }

        public void Move(Point offset)
        {
            for (int i = 0; i < _points.Length; i++)
                _points[i] = new Point(_points[i].X + offset.X, _points[i].Y + offset.Y);
        }

        public void Scale(double ratio)
        {
            if (ratio == 0)
                throw new ArgumentOutOfRangeException(nameof(ratio), "Масштаб не должен быть равен нулю");
            Point center = GetCenter();
            for (int i = 0; i < _points.Length; i++)
                _points[i] = new Point(
                    center.X + (_points[i].X - center.X) * ratio,
                    center.Y + (_points[i].Y - center.Y) * ratio);
        }

        public void Rotate(double angle)
        {
            Point center = GetCenter();
            double radians = angle * Math.PI / 180.0;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            for (int i = 0; i < _points.Length; i++)
            {
                double dx = _points[i].X - center.X;
                double dy = _points[i].Y - center.Y;
                _points[i] = new Point(
                    center.X + dx * cos - dy * sin,
                    center.Y + dx * sin + dy * cos);
            }
        }

        public void MirrorX()
        {
            Point center = GetCenter();
            for (int i = 0; i < _points.Length; i++)
                _points[i] = new Point(
                    center.X - (_points[i].X - center.X),
                    _points[i].Y);
        }

        public void MirrorY()
        {
            Point center = GetCenter();
            for (int i = 0; i < _points.Length; i++)
                _points[i] = new Point(
                    _points[i].X,
                    center.Y - (_points[i].Y - center.Y));
        }
    }
}
