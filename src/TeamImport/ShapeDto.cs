using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common;
using GraphicEditor.TeamTools.Shapes;
using GraphicEditor.ViewModels;

namespace GraphicEditor.TeamImport;

public record ShapeDto
{
    public string Type { get; init; } = "";
    public string Name { get; init; } = "";
    public string FillColor { get; init; } = "#FF6495ED";
    public string StrokeColor { get; init; } = "#FF000000";
    public double Opacity { get; init; } = 1.0;
    public string LayerName { get; init; } = EditorConstants.DefaultLayerName;
    public bool IsVisible { get; init; } = true;
    public double StrokeWidth { get; init; } = 1.5;
    public double RotationAngle { get; init; }

    public double CenterX { get; init; }
    public double CenterY { get; init; }
    public double Radius { get; init; }
    public double? RadiusY { get; init; }

    public double[]? PointsX { get; init; }
    public double[]? PointsY { get; init; }
    public double? RotationCenterX { get; init; }
    public double? RotationCenterY { get; init; }

    public static ShapeDto FromViewModel(ShapeViewModel vm)
    {
        var dto = new ShapeDto
        {
            Type = vm.ShapeType,
            Name = vm.Name,
            FillColor = ColorToHex(vm.FillColor),
            StrokeColor = ColorToHex(vm.StrokeColor),
            Opacity = vm.Opacity,
            LayerName = vm.LayerName,
            IsVisible = vm.IsVisible,
            StrokeWidth = vm.StrokeWidth,
            RotationAngle = vm.RotationAngle,
        };

        if (vm is CircleViewModel cv)
        {
            dto = dto with
            {
                CenterX = cv.Model.Center.X,
                CenterY = cv.Model.Center.Y,
                Radius = cv.Model.RadiusX,
                RadiusY = cv.Model.RadiusY,
            };
        }
        else if (vm is PolygonViewModel pv)
        {
            var pts = pv.Model.Points;
            var xs = new double[pts.Length];
            var ys = new double[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                xs[i] = pts[i].X;
                ys[i] = pts[i].Y;
            }

            var center = pv.Model.Center;
            dto = dto with
            {
                PointsX = xs,
                PointsY = ys,
                RotationCenterX = center.X,
                RotationCenterY = center.Y,
            };
        }

        return dto;
    }

    public ShapeViewModel? ToViewModel()
    {
        var fill = ParseColor(FillColor);
        var stroke = ParseColor(StrokeColor);

        Point? savedCenter = RotationCenterX.HasValue && RotationCenterY.HasValue
            ? new Point(RotationCenterX.Value, RotationCenterY.Value)
            : null;

        ShapeViewModel? vm = Type switch
        {
            "Circle" => new CircleViewModel(
                new Circle(new Point(CenterX, CenterY),
                    Radius > 0 ? Radius : 10,
                    RadiusY.HasValue && RadiusY.Value > 0 ? RadiusY.Value : (Radius > 0 ? Radius : 10))),

            "Rectangle" when PointsX?.Length >= 4 && PointsY?.Length >= 4 =>
                CreatePolygonVm(
                    savedCenter.HasValue
                        ? new Quadrilateral(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1]),
                            new Point(PointsX[2], PointsY[2]),
                            new Point(PointsX[3], PointsY[3]),
                            savedCenter.Value)
                        : new Quadrilateral(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1]),
                            new Point(PointsX[2], PointsY[2]),
                            new Point(PointsX[3], PointsY[3])),
                    "Rectangle"),

            "Rectangle" when PointsX?.Length >= 2 && PointsY?.Length >= 2 =>
                new PolygonViewModel(
                    new Rectangle(
                        new Point(PointsX[0], PointsY[0]),
                        new Point(PointsX[1], PointsY[1])),
                    "Rectangle", Name),

            "Triangle" when PointsX?.Length >= 3 && PointsY?.Length >= 3 =>
                CreatePolygonVm(
                    savedCenter.HasValue
                        ? new Triangle(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1]),
                            new Point(PointsX[2], PointsY[2]),
                            savedCenter.Value)
                        : new Triangle(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1]),
                            new Point(PointsX[2], PointsY[2])),
                    "Triangle"),

            "Line" when PointsX?.Length >= 2 && PointsY?.Length >= 2 =>
                CreatePolygonVm(
                    savedCenter.HasValue
                        ? new Line(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1]),
                            savedCenter.Value)
                        : new Line(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1])),
                    "Line"),

            _ => null,
        };

        if (vm is not null)
        {
            vm.Name = Name;
            vm.FillColor = fill;
            vm.StrokeColor = stroke;
            vm.Opacity = Opacity;
            vm.LayerName = LayerName;
            vm.IsVisible = IsVisible;
            vm.StrokeWidth = StrokeWidth;
            vm.RotationAngle = RotationAngle;
        }

        return vm;
    }

    private PolygonViewModel CreatePolygonVm(Polygon polygon, string type) =>
        new(polygon, type, Name);

    private static string ColorToHex(Color c) =>
        $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    private static Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); }
        catch { return Colors.CornflowerBlue; }
    }
}
