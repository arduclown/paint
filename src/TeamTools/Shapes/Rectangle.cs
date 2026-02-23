using System;
using System.Globalization;
using System.Text;
using Avalonia;

namespace GraphicEditor.TeamTools.Shapes
{
    public class Rectangle : Quadrilateral
    {
        public Rectangle(Point first, Point second)
            : base(new Point(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y)),
                   new Point(Math.Max(first.X, second.X), Math.Min(first.Y, second.Y)),
                   new Point(Math.Max(first.X, second.X), Math.Max(first.Y, second.Y)),
                   new Point(Math.Min(first.X, second.X), Math.Max(first.Y, second.Y)))
        { }

        public override string SerializedData => BuildPath();

        private string BuildPath()
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
