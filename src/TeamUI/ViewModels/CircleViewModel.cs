using Avalonia;
using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.ViewModels
{
    public class CircleViewModel : ShapeViewModel
    {
        private readonly Circle _circle;

        public CircleViewModel(Circle circle)
        {
            _circle = circle;
        }

        public override string ShapeType => "Circle";

        public Circle Model => _circle;

        public override string Geometry => _circle.SerializedData;

        public override Rect Bounds => new Rect(
            _circle.Center.X - _circle.Radius,
            _circle.Center.Y - _circle.Radius,
            _circle.Radius * 2,
            _circle.Radius * 2);

        public override void Move(Point delta)
        {
            _circle.Move(delta);
            NotifyGeometryChanged();
        }

        public override void Scale(double ratio)
        {
            _circle.Scale(ratio);
            NotifyGeometryChanged();
        }

        public override void Rotate(double angle)
        {
            _circle.Rotate(angle);
            NotifyGeometryChanged();
        }

        public override void MirrorX()
        {
            _circle.MirrorX();
            NotifyGeometryChanged();
        }

        public override void MirrorY()
        {
            _circle.MirrorY();
            NotifyGeometryChanged();
        }
    }
}
