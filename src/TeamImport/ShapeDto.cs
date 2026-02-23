using System;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.TeamTools.Shapes;
using GraphicEditor.ViewModels;

namespace GraphicEditor.TeamImport
{
    // DTO для сериализации/десериализации фигур в JSON
    public class ShapeDto
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string FillColor { get; set; } = "#FF6495ED";
        public string StrokeColor { get; set; } = "#FF000000";
        public double Opacity { get; set; } = 1.0;
        public string LayerName { get; set; } = "Слой 1";
        public bool IsVisible { get; set; } = true;

        // Для Circle
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Radius { get; set; }

        // Для Polygon (Rectangle, Triangle, Line)
        public double[]? PointsX { get; set; }
        public double[]? PointsY { get; set; }

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
            };

            if (vm is CircleViewModel cv)
            {
                dto.CenterX = cv.Model.Center.X;
                dto.CenterY = cv.Model.Center.Y;
                dto.Radius = cv.Model.Radius;
            }
            else if (vm is PolygonViewModel pv)
            {
                var pts = pv.Model.Points;
                dto.PointsX = new double[pts.Length];
                dto.PointsY = new double[pts.Length];
                for (int i = 0; i < pts.Length; i++)
                {
                    dto.PointsX[i] = pts[i].X;
                    dto.PointsY[i] = pts[i].Y;
                }
            }

            return dto;
        }

        public ShapeViewModel? ToViewModel()
        {
            var fill = ParseColor(FillColor);
            var stroke = ParseColor(StrokeColor);

            ShapeViewModel? vm = Type switch
            {
                "Circle" => new CircleViewModel(new Circle(new Point(CenterX, CenterY), Radius > 0 ? Radius : 10)),
                "Rectangle" when PointsX?.Length >= 2 && PointsY?.Length >= 2 =>
                    new PolygonViewModel(
                        new Rectangle(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[2], PointsY[2])),
                        "Rectangle", Name),
                "Triangle" when PointsX?.Length >= 3 && PointsY?.Length >= 3 =>
                    new PolygonViewModel(
                        new Triangle(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1]),
                            new Point(PointsX[2], PointsY[2])),
                        "Triangle", Name),
                "Line" when PointsX?.Length >= 2 && PointsY?.Length >= 2 =>
                    new PolygonViewModel(
                        new Line(
                            new Point(PointsX[0], PointsY[0]),
                            new Point(PointsX[1], PointsY[1])),
                        "Line", Name),
                _ => null
            };

            if (vm != null)
            {
                vm.Name = Name;
                vm.FillColor = fill;
                vm.StrokeColor = stroke;
                vm.Opacity = Opacity;
                vm.LayerName = LayerName;
                vm.IsVisible = IsVisible;
            }

            return vm;
        }

        private static string ColorToHex(Color c) =>
            $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

        private static Color ParseColor(string hex)
        {
            try { return Color.Parse(hex); }
            catch { return Colors.CornflowerBlue; }
        }
    }
}
