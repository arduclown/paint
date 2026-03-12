using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.Tests.Fakes;

/// <summary>
/// Простая тестовая реализация ISceneShape.
/// Накапливает применённые трансформации для проверки в тестах.
/// </summary>
public class FakeShape : ISceneShape
{
    public string LayerName { get; set; } = "Layer1";
    public bool IsVisible { get; set; } = true;
    public Color FillColor { get; set; } = Colors.Red;
    public Color StrokeColor { get; set; } = Colors.Black;

    // Накопленная позиция (сумма всех вызовов Move)
    public double X { get; private set; }
    public double Y { get; private set; }

    // Накопленный коэффициент масштаба (произведение всех вызовов Scale)
    public double ScaleFactor { get; private set; } = 1.0;

    // Накопленный угол поворота (сумма всех вызовов Rotate)
    public double Angle { get; private set; }

    public void Move(Point delta)
    {
        X += delta.X;
        Y += delta.Y;
    }

    public void Scale(double ratio) => ScaleFactor *= ratio;

    public void Rotate(double angle) => Angle += angle;

    public void MirrorX() { }
    public void MirrorY() { }
}
