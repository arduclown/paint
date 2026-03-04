using System;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes;

public class Rectangle : Quadrilateral
{
    public Rectangle(Point first, Point second)
        : base(new Point(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y)),
               new Point(Math.Max(first.X, second.X), Math.Min(first.Y, second.Y)),
               new Point(Math.Max(first.X, second.X), Math.Max(first.Y, second.Y)),
               new Point(Math.Min(first.X, second.X), Math.Max(first.Y, second.Y)))
    { }

    public Rectangle(Point first, Point second, Point center)
        : base(new Point(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y)),
               new Point(Math.Max(first.X, second.X), Math.Min(first.Y, second.Y)),
               new Point(Math.Max(first.X, second.X), Math.Max(first.Y, second.Y)),
               new Point(Math.Min(first.X, second.X), Math.Max(first.Y, second.Y)),
               center)
    { }
}
