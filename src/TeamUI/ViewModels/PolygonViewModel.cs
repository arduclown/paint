using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia;
using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.ViewModels
{
    public class PolygonViewModel : ShapeViewModel
    {
        private readonly Polygon _polygon;
        private readonly string _shapeType;

        public PolygonViewModel(Polygon polygon, string shapeType, string name)
        {
            _polygon = polygon;
            _shapeType = shapeType;
            Name = name;
        }

        public override string ShapeType => _shapeType;

        public override Rect Bounds
        {
            get
            {
                var pts = _polygon.Points;
                double minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
                double minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public Polygon Model => _polygon;

        public override string Geometry
        {
            get
            {
                var ci = CultureInfo.InvariantCulture;
                var pts = _polygon.Points;
                if (pts.Length == 0) return "";

                var sb = new StringBuilder();
                sb.AppendFormat(ci, "M {0:F2},{1:F2}", pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Length; i++)
                    sb.AppendFormat(ci, " L {0:F2},{1:F2}", pts[i].X, pts[i].Y);

                // Линия не закрывается
                if (_shapeType != "Line")
                    sb.Append(" Z");

                return sb.ToString();
            }
        }

        public override void Move(Point delta)
        {
            _polygon.Move(delta);
            NotifyGeometryChanged();
        }

        public override void Scale(double ratio)
        {
            _polygon.Scale(ratio);
            NotifyGeometryChanged();
        }

        public override void Rotate(double angle)
        {
            _polygon.Rotate(angle);
            NotifyGeometryChanged();
        }

        public override void MirrorX()
        {
            _polygon.MirrorX();
            NotifyGeometryChanged();
        }

        public override void MirrorY()
        {
            _polygon.MirrorY();
            NotifyGeometryChanged();
        }
    }
}
