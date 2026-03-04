using Avalonia;

namespace GraphicEditor.Common.Interfaces;

public interface IShape
{
    string SerializedData { get; }
    void Move(Point offset);
    void Scale(double ratio);
    void Scale(double ratioX, double ratioY);
    void Rotate(double angle);
    void MirrorX();
    void MirrorY();
}
