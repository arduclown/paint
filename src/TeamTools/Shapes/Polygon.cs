using System;
using System.Text;
using Avalonia;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamTools.Shapes;

public abstract class Polygon : IShape
{
    private Point[] _points;
    private Point _center;

    public Point[] Points => _points;
    public Point Center => _center;

    protected Polygon(Point[] points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length == 0)
            throw new ArgumentException("Коллекция точек не должна быть пустой", nameof(points));

        _points = points;
        _center = ComputeCentroid(points);
    }

    protected Polygon(Point[] points, Point center)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Length == 0)
            throw new ArgumentException("Коллекция точек не должна быть пустой", nameof(points));

        _points = points;
        _center = center;
    }

    protected virtual bool IsClosed => true;

    public string SerializedData
    {
        get
        {
            var pts = _points;
            var sb = new StringBuilder();
            sb.Append(FormattableString.Invariant($"M {pts[0].X:F2},{pts[0].Y:F2}"));
            for (int i = 1; i < pts.Length; i++)
                sb.Append(FormattableString.Invariant($" L {pts[i].X:F2},{pts[i].Y:F2}"));
            if (IsClosed)
                sb.Append(" Z");
            return sb.ToString();
        }
    }

    private static Point ComputeCentroid(Point[] pts)
    {
        double sx = 0, sy = 0;
        foreach (var p in pts) { sx += p.X; sy += p.Y; }
        return new Point(sx / pts.Length, sy / pts.Length);
    }

    public void Move(Point offset)
    {
        for (int i = 0; i < _points.Length; i++)
            _points[i] = new Point(_points[i].X + offset.X, _points[i].Y + offset.Y);
        _center = new Point(_center.X + offset.X, _center.Y + offset.Y);
    }

    public void Scale(double ratio)
    {
        if (ratio == 0)
            throw new ArgumentOutOfRangeException(nameof(ratio));

        for (int i = 0; i < _points.Length; i++)
            _points[i] = new Point(
                _center.X + (_points[i].X - _center.X) * ratio,
                _center.Y + (_points[i].Y - _center.Y) * ratio);
    }

    public void Scale(double ratioX, double ratioY)
    {
        if (ratioX == 0) throw new ArgumentOutOfRangeException(nameof(ratioX));
        if (ratioY == 0) throw new ArgumentOutOfRangeException(nameof(ratioY));

        for (int i = 0; i < _points.Length; i++)
            _points[i] = new Point(
                _center.X + (_points[i].X - _center.X) * ratioX,
                _center.Y + (_points[i].Y - _center.Y) * ratioY);
    }

    public void Rotate(double angle)
    {
        double radians = angle * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        for (int i = 0; i < _points.Length; i++)
        {
            double dx = _points[i].X - _center.X;
            double dy = _points[i].Y - _center.Y;
            _points[i] = new Point(
                _center.X + dx * cos - dy * sin,
                _center.Y + dx * sin + dy * cos);
        }
    }

    public void MirrorX()
    {
        for (int i = 0; i < _points.Length; i++)
            _points[i] = new Point(
                _center.X - (_points[i].X - _center.X),
                _points[i].Y);
    }

    public void MirrorY()
    {
        for (int i = 0; i < _points.Length; i++)
            _points[i] = new Point(
                _points[i].X,
                _center.Y - (_points[i].Y - _center.Y));
    }
}
