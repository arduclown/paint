using Avalonia;
using Avalonia.Media;
using GraphicEditor.TeamCore;

namespace GraphicEditor.ViewModels
{
    // Координирует полный цикл создания фигуры:
    //   1) бекенд (ShapeFactory) строит доменный объект IShape
    //   2) фронт (ShapeViewModelFactory) оборачивает его в ViewModel
    //   3) применяется UI-контекст (цвет, слой, имя)
    public class ShapeCreationService
    {
        private int _circleCount, _rectCount, _triCount, _lineCount;

        public ShapeViewModel Create(
            ToolType tool, Point p1, Point p2,
            Color fill, Color stroke,
            string layerName, bool isVisible)
        {
            ShapeViewModel vm = tool switch
            {
                ToolType.Circle =>
                    ShapeViewModelFactory.CreateCircle(
                        ShapeFactory.CreateCircle(p1, p2),
                        $"Круг {++_circleCount}"),

                ToolType.Rectangle =>
                    ShapeViewModelFactory.CreateRectangle(
                        ShapeFactory.CreateRectangle(p1, p2),
                        $"Прямоугольник {++_rectCount}"),

                ToolType.Triangle =>
                    ShapeViewModelFactory.CreateTriangle(
                        ShapeFactory.CreateTriangle(p1, p2),
                        $"Треугольник {++_triCount}"),

                _ =>
                    ShapeViewModelFactory.CreateLine(
                        ShapeFactory.CreateLine(p1, p2),
                        $"Линия {++_lineCount}"),
            };

            vm.FillColor   = fill;
            vm.StrokeColor = stroke;
            vm.LayerName   = layerName;
            vm.IsVisible   = isVisible;
            return vm;
        }

        public void ResetCounters() =>
            _circleCount = _rectCount = _triCount = _lineCount = 0;
    }
}
