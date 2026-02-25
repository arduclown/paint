namespace GraphicEditor.Common.Interfaces;

public interface ILayer
{
    string Name { get; set; }
    bool IsVisible { get; set; }
    bool IsLocked { get; set; }
    bool IsActive { get; set; }
}
