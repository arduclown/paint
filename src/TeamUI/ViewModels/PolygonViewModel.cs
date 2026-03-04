using System.Linq;
using Avalonia;
using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.ViewModels;

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
    public Polygon Model => _polygon;

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

    public override string Geometry => _polygon.SerializedData;

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

    public override void Scale(double ratioX, double ratioY)
    {
        _polygon.Scale(ratioX, ratioY);
        NotifyGeometryChanged();
    }

    public override void Rotate(double angle)
    {
        RotationAngle += angle;
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
