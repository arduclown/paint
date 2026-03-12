using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.ViewModels;

public static class ShapeViewModelFactory
{
    public static CircleViewModel CreateCircle(Circle circle, string name) =>
        new(circle) { Name = name };

    public static PolygonViewModel CreateRectangle(Rectangle rectangle, string name) =>
        new(rectangle, "Rectangle", name);

    public static PolygonViewModel CreateTriangle(Triangle triangle, string name) =>
        new(triangle, "Triangle", name);

    public static PolygonViewModel CreateLine(Line line, string name) =>
        new(line, "Line", name);
}