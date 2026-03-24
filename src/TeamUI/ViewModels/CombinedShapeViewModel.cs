using System;
using Avalonia;
using Avalonia.Media;

namespace GraphicEditor.ViewModels;

public class CombinedShapeViewModel : ShapeViewModel
{
    private readonly ShapeViewModel _shape1;
    private readonly ShapeViewModel _shape2;
    private readonly GeometryCombineMode _combineMode;

    public CombinedShapeViewModel(ShapeViewModel shape1, ShapeViewModel shape2, GeometryCombineMode combineMode)
    {
        _shape1 = shape1;
        _shape2 = shape2;
        _combineMode = combineMode;

        FillColor = shape1.FillColor;
        StrokeColor = shape1.StrokeColor;
        Name = combineMode switch
        {
            GeometryCombineMode.Union     => $"{shape1.Name} ∪ {shape2.Name}",
            GeometryCombineMode.Intersect => $"{shape1.Name} ∩ {shape2.Name}",
            GeometryCombineMode.Exclude   => $"{shape1.Name} - {shape2.Name}",
            GeometryCombineMode.Xor       => $"{shape1.Name} ⊕ {shape2.Name}",
            _                             => "Комбинация",
        };
    }

    public override string ShapeType => "Combined";

    // Для SVG-экспорта — объединяем path-строки обеих фигур
    public override string Geometry => $"{_shape1.Geometry} {_shape2.Geometry}";

    public override Avalonia.Media.Geometry GeometryData
    {
        get
        {
            if (_combineMode == GeometryCombineMode.Exclude)
            {
                // GeometryGroup с правилом EvenOdd корректно рендерит «дыру»:
                // область пересечения вычитается из первой фигуры.
                return new GeometryGroup
                {
                    FillRule = FillRule.EvenOdd,
                    Children = [_shape1.GeometryData, _shape2.GeometryData],
                };
            }

            return new CombinedGeometry
            {
                GeometryCombineMode = _combineMode,
                Geometry1 = _shape1.GeometryData,
                Geometry2 = _shape2.GeometryData,
            };
        }
    }

    public override Rect Bounds
    {
        get
        {
            var b1 = _shape1.Bounds;
            var b2 = _shape2.Bounds;

            if (_combineMode == GeometryCombineMode.Intersect)
            {
                // Для пересечения bbox — это пересечение прямоугольников
                double ix = Math.Max(b1.X, b2.X);
                double iy = Math.Max(b1.Y, b2.Y);
                double ir = Math.Min(b1.Right, b2.Right);
                double ib = Math.Min(b1.Bottom, b2.Bottom);
                if (ir > ix && ib > iy)
                    return new Rect(ix, iy, ir - ix, ib - iy);
                // Фигуры не пересекаются — берём bbox первой
                return b1;
            }

            if (_combineMode == GeometryCombineMode.Exclude)
            {
                // Для вычитания bbox — это bbox первой фигуры
                return b1;
            }

            // Union / Xor — объединение bbox
            double x      = Math.Min(b1.X, b2.X);
            double y      = Math.Min(b1.Y, b2.Y);
            double right  = Math.Max(b1.Right, b2.Right);
            double bottom = Math.Max(b1.Bottom, b2.Bottom);
            return new Rect(x, y, right - x, bottom - y);
        }
    }

    // Общий центр обеих фигур — вокруг него выполняются все трансформации
    private Point CombinedCenter
    {
        get
        {
            var b = Bounds;
            return new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
        }
    }

    private static Point ShapeCenter(ShapeViewModel s)
    {
        var b = s.Bounds;
        return new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
    }

    public override void Move(Point delta)
    {
        _shape1.Move(delta);
        _shape2.Move(delta);
        NotifyGeometryChanged();
    }

    // Масштабирование вокруг общего центра:
    // сначала сдвигаем каждую фигуру так, чтобы её центр оказался
    // в правильной позиции относительно общего центра после масштаба,
    // затем масштабируем каждую вокруг её собственного (уже сдвинутого) центра.
    public override void Scale(double ratio)
    {
        var pivot = CombinedCenter;
        ScaleAroundPivot(_shape1, ratio, pivot);
        ScaleAroundPivot(_shape2, ratio, pivot);
        NotifyGeometryChanged();
    }

    public override void ScaleXY(double sx, double sy)
    {
        var pivot = CombinedCenter;
        ScaleXYAroundPivot(_shape1, sx, sy, pivot);
        ScaleXYAroundPivot(_shape2, sx, sy, pivot);
        NotifyGeometryChanged();
    }

    // Поворот вокруг общего центра
    public override void Rotate(double angle)
    {
        var pivot = CombinedCenter;
        RotateAroundPivot(_shape1, angle, pivot);
        RotateAroundPivot(_shape2, angle, pivot);
        RotationAngle += angle;
        NotifyGeometryChanged();
    }

    // Отражение по X вокруг вертикальной оси общего центра
    public override void MirrorX()
    {
        var pivot = CombinedCenter;
        MirrorXAroundPivot(_shape1, pivot);
        MirrorXAroundPivot(_shape2, pivot);
        NotifyGeometryChanged();
    }

    // Отражение по Y вокруг горизонтальной оси общего центра
    public override void MirrorY()
    {
        var pivot = CombinedCenter;
        MirrorYAroundPivot(_shape1, pivot);
        MirrorYAroundPivot(_shape2, pivot);
        NotifyGeometryChanged();
    }

    // ── Вспомогательные методы ─────────────────────────────────────────────

    private static void RotateAroundPivot(ShapeViewModel shape, double angle, Point pivot)
    {
        double radians = angle * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        var center = ShapeCenter(shape);
        double dx = center.X - pivot.X;
        double dy = center.Y - pivot.Y;

        // Куда перемещается центр после поворота вокруг pivot
        double newCx = pivot.X + dx * cos - dy * sin;
        double newCy = pivot.Y + dx * sin + dy * cos;

        shape.Move(new Point(newCx - center.X, newCy - center.Y));
        shape.Rotate(angle);
    }

    private static void ScaleXYAroundPivot(ShapeViewModel shape, double sx, double sy, Point pivot)
    {
        var center = ShapeCenter(shape);
        double newCx = pivot.X + (center.X - pivot.X) * sx;
        double newCy = pivot.Y + (center.Y - pivot.Y) * sy;
        shape.Move(new Point(newCx - center.X, newCy - center.Y));
        shape.ScaleXY(sx, sy);
    }

    private static void ScaleAroundPivot(ShapeViewModel shape, double ratio, Point pivot)
    {
        var center = ShapeCenter(shape);

        // Куда перемещается центр после масштаба вокруг pivot
        double newCx = pivot.X + (center.X - pivot.X) * ratio;
        double newCy = pivot.Y + (center.Y - pivot.Y) * ratio;

        shape.Move(new Point(newCx - center.X, newCy - center.Y));
        shape.Scale(ratio);
    }

    private static void MirrorXAroundPivot(ShapeViewModel shape, Point pivot)
    {
        var center = ShapeCenter(shape);

        // Отражаем центр относительно вертикальной оси pivot.X
        double newCx = 2 * pivot.X - center.X;
        shape.Move(new Point(newCx - center.X, 0));
        shape.MirrorX();
    }

    private static void MirrorYAroundPivot(ShapeViewModel shape, Point pivot)
    {
        var center = ShapeCenter(shape);

        // Отражаем центр относительно горизонтальной оси pivot.Y
        double newCy = 2 * pivot.Y - center.Y;
        shape.Move(new Point(0, newCy - center.Y));
        shape.MirrorY();
    }
}