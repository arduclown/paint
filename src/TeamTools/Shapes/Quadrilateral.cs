using System;
using System.Text;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes;

public class Quadrilateral : Polygon
{
    public Quadrilateral(Point p1, Point p2, Point p3, Point p4)
        : base([p1, p2, p3, p4]) { }

    public Quadrilateral(Point p1, Point p2, Point p3, Point p4, Point center)
        : base([p1, p2, p3, p4], center) { }

    public override string SerializedData
    {
        get
        {
            var pts = Points;
            var sb = new StringBuilder();
            sb.Append(FormattableString.Invariant($"M {pts[0].X:F2},{pts[0].Y:F2}"));
            for (int i = 1; i < pts.Length; i++)
                sb.Append(FormattableString.Invariant($" L {pts[i].X:F2},{pts[i].Y:F2}"));
            sb.Append(" Z");
            return sb.ToString();
        }
    }
}
