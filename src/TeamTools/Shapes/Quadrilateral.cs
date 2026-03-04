using Avalonia;

namespace GraphicEditor.TeamTools.Shapes;

public class Quadrilateral : Polygon
{
    public Quadrilateral(Point p1, Point p2, Point p3, Point p4)
        : base([p1, p2, p3, p4]) { }

    public Quadrilateral(Point p1, Point p2, Point p3, Point p4, Point center)
        : base([p1, p2, p3, p4], center) { }
}
