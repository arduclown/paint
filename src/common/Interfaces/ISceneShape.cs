using Avalonia;
using Avalonia.Media;

namespace GraphicEditor.Common.Interfaces;

public interface ISceneShape
{
    string LayerName { get; set; }
    bool IsVisible { get; set; }
    Color FillColor { get; set; }
    Color StrokeColor { get; set; }

    void Move(Point delta);
    void Scale(double ratio);
    void Rotate(double angle);
    void MirrorX();
    void MirrorY();
}
