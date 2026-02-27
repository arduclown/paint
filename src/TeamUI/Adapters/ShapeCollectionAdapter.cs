using System.Collections.ObjectModel;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.ViewModels;

/// <summary>
/// Адаптер: оборачивает ObservableCollection как ISceneCollection,
/// чтобы команды из TeamCore работали с UI-коллекцией без прямой зависимости.
/// </summary>
public class ShapeCollectionAdapter(ObservableCollection<ShapeViewModel> inner) : ISceneCollection
{
    public void Add(ISceneShape shape) => inner.Add((ShapeViewModel)shape);
    public void Remove(ISceneShape shape) => inner.Remove((ShapeViewModel)shape);
    public int IndexOf(ISceneShape shape) => inner.IndexOf((ShapeViewModel)shape);
    public void Insert(int index, ISceneShape shape) => inner.Insert(index, (ShapeViewModel)shape);
    public void RemoveAt(int index) => inner.RemoveAt(index);
}
