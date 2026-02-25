using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.ViewModels;

// Создаёт ViewModel-обёртки вокруг доменных объектов фигур (IShape)
public static class ShapeViewModelFactory
{
    public static CircleViewModel CreateCircle(Circle circle, string name) =>
        new CircleViewModel(circle) { Name = name };

    public static PolygonViewModel CreateRectangle(Rectangle rectangle, string name) =>
        new PolygonViewModel(rectangle, "Rectangle", name);

    public static PolygonViewModel CreateTriangle(Triangle triangle, string name) =>
        new PolygonViewModel(triangle, "Triangle", name);

    public static PolygonViewModel CreateLine(Line line, string name) =>
        new PolygonViewModel(line, "Line", name);
}
