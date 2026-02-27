using Avalonia;
using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.ViewModels;

public class CircleViewModel(Circle circle) : ShapeViewModel
{
    public override string ShapeType => "Circle";
    public Circle Model => circle;

    public override string Geometry => circle.SerializedData;

    public override Rect Bounds => new(
        circle.Center.X - circle.Radius,
        circle.Center.Y - circle.Radius,
        circle.Radius * 2,
        circle.Radius * 2);

    public override void Move(Point delta)
    {
        circle.Move(delta);
        NotifyGeometryChanged();
    }

    public override void Scale(double ratio)
    {
        circle.Scale(ratio);
        NotifyGeometryChanged();
    }

    public override void Rotate(double angle)
    {
        RotationAngle += angle;
        circle.Rotate(angle);
        NotifyGeometryChanged();
    }

    public override void MirrorX()
    {
        circle.MirrorX();
        NotifyGeometryChanged();
    }

    public override void MirrorY()
    {
        circle.MirrorY();
        NotifyGeometryChanged();
    }
}
