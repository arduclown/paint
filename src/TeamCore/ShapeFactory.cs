using System;
using Avalonia;
using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.TeamCore
{
    // Создаёт доменные объекты фигур (IShape) по двум опорным точкам.
    // Не зависит от UI — чистый бекенд.
    public static class ShapeFactory
    {
        private const double MinSize = 5.0;

        public static Circle CreateCircle(Point center, Point edge)
        {
            double dx = edge.X - center.X, dy = edge.Y - center.Y;
            double r = Math.Max(MinSize, Math.Sqrt(dx * dx + dy * dy));
            return new Circle(center, r);
        }

        public static Rectangle CreateRectangle(Point p1, Point p2)
        {
            double x2 = Math.Abs(p2.X - p1.X) < MinSize ? p1.X + MinSize : p2.X;
            double y2 = Math.Abs(p2.Y - p1.Y) < MinSize ? p1.Y + MinSize : p2.Y;
            return new Rectangle(p1, new Point(x2, y2));
        }

        public static Triangle CreateTriangle(Point p1, Point p2)
        {
            double x1 = Math.Min(p1.X, p2.X), x2 = Math.Max(p1.X, p2.X);
            double y1 = Math.Min(p1.Y, p2.Y), y2 = Math.Max(p1.Y, p2.Y);
            if (x2 - x1 < MinSize) x2 = x1 + MinSize;
            if (y2 - y1 < MinSize) y2 = y1 + MinSize;
            return new Triangle(
                new Point((x1 + x2) / 2.0, y1),
                new Point(x1, y2),
                new Point(x2, y2));
        }

        public static Line CreateLine(Point p1, Point p2)
        {
            if (Math.Abs(p2.X - p1.X) + Math.Abs(p2.Y - p1.Y) < MinSize)
                p2 = new Point(p1.X + MinSize, p1.Y);
            return new Line(p1, p2);
        }
    }
}
