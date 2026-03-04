using Avalonia;

namespace GraphicEditor.TeamTools.Shapes;

public class Triangle : Polygon
{
    public Triangle(Point p1, Point p2, Point p3) : base([p1, p2, p3]) { }

    public Triangle(Point p1, Point p2, Point p3, Point center) : base([p1, p2, p3], center) { }
}
