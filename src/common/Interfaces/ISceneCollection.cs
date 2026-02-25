namespace GraphicEditor.Common.Interfaces;

public interface ISceneCollection
{
    void Add(ISceneShape shape);
    void Remove(ISceneShape shape);
    int IndexOf(ISceneShape shape);
    void Insert(int index, ISceneShape shape);
    void RemoveAt(int index);
}
