using System;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes;

public class Line : Polygon
{
    public Line(Point p1, Point p2) : base([p1, p2]) { }

    public Line(Point p1, Point p2, Point center) : base([p1, p2], center) { }

    public override string SerializedData
    {
        get
        {
            var pts = Points;
            return FormattableString.Invariant(
                $"M {pts[0].X:F2},{pts[0].Y:F2} L {pts[1].X:F2},{pts[1].Y:F2}");
        }
    }
}
