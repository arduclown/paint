using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.Tests.Fakes;

/// <summary>
/// Простая тестовая реализация ISceneCollection на основе List.
/// </summary>
public class FakeCollection : ISceneCollection
{
    private readonly List<ISceneShape> _shapes = new();

    public IReadOnlyList<ISceneShape> Shapes => _shapes;
    public int Count => _shapes.Count;
    public ISceneShape this[int index] => _shapes[index];

    public void Add(ISceneShape shape) => _shapes.Add(shape);
    public void Remove(ISceneShape shape) => _shapes.Remove(shape);
    public int IndexOf(ISceneShape shape) => _shapes.IndexOf(shape);
    public void Insert(int index, ISceneShape shape) => _shapes.Insert(index, shape);
    public void RemoveAt(int index) => _shapes.RemoveAt(index);
}
