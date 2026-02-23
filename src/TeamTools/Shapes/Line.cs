using System.Globalization;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes
{
    public class Line : Polygon
    {
        public Line(Point p1, Point p2) : base(new Point[] { p1, p2 }) { }

        public override string SerializedData
        {
            get
            {
                var ci = CultureInfo.InvariantCulture;
                var pts = Points;
                return string.Format(ci, "M {0:F2},{1:F2} L {2:F2},{3:F2}",
                    pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);
            }
        }
    }
}
