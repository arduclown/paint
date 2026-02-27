using System;
using Avalonia;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.TeamTools.Shapes;

/// <summary>
/// Базовый класс для полигональных фигур.
/// Хранит стабильный центр вращения — настоящий центроид,
/// который не дрейфует при повторных поворотах.
/// </summary>
public abstract class Polygon : IShape
{
    private Point[] _points;

    // Стабильный центр вращения/масштабирования (центроид)
    private Point _center;

    public Point[] Points => _points;

    /// <summary>Центр вращения (центроид точек).</summary>
    public Point Center => _center;

    protected Polygon(Point[] points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (points.Length == 0)
            throw new ArgumentException("Коллекция точек не должна быть пустой", nameof(points));

        _points = points;
        _center = ComputeCentroid(points);
    }

    /// <summary>Конструктор с явным центром — для десериализации.</summary>
    protected Polygon(Point[] points, Point center)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        if (points.Length == 0)
            throw new ArgumentException("Коллекция точек не должна быть пустой", nameof(points));

        _points = points;
        _center = center;
    }

    public abstract string SerializedData { get; }

    /// <summary>Вычисляет центроид как среднее арифметическое всех точек.</summary>
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
