using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.Tests.Fakes;

public class FakeLayer : ILayer
{
    public string Name { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; }

    public FakeLayer(string name) => Name = name;
}
