using System.Globalization;
using System.Text;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes
{
    public class Quadrilateral : Polygon
    {
        public Quadrilateral(Point p1, Point p2, Point p3, Point p4)
            : base(new Point[] { p1, p2, p3, p4 }) { }

        public override string SerializedData
        {
            get
            {
                var ci = CultureInfo.InvariantCulture;
                var pts = Points;
                var sb = new StringBuilder();
                sb.AppendFormat(ci, "M {0:F2},{1:F2}", pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Length; i++)
                    sb.AppendFormat(ci, " L {0:F2},{1:F2}", pts[i].X, pts[i].Y);
                sb.Append(" Z");
                return sb.ToString();
            }
        }
    }
}
