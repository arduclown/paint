using Avalonia;

namespace GraphicEditor.TeamTools.Shapes;

public class Line : Polygon
{
    public Line(Point p1, Point p2) : base([p1, p2]) { }

    public Line(Point p1, Point p2, Point center) : base([p1, p2], center) { }

    protected override bool IsClosed => false;
}
