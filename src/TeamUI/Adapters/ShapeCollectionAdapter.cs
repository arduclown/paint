using System.Collections.ObjectModel;
using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.ViewModels
{
    // Адаптер: оборачивает ObservableCollection<ShapeViewModel> как ISceneCollection,
    // чтобы команды из TeamCore могли работать с коллекцией UI без прямой зависимости на TeamUI.
    public class ShapeCollectionAdapter : ISceneCollection
    {
        private readonly ObservableCollection<ShapeViewModel> _inner;

        public ShapeCollectionAdapter(ObservableCollection<ShapeViewModel> inner)
        {
            _inner = inner;
        }

        public void Add(ISceneShape shape) => _inner.Add((ShapeViewModel)shape);
        public void Remove(ISceneShape shape) => _inner.Remove((ShapeViewModel)shape);
        public int IndexOf(ISceneShape shape) => _inner.IndexOf((ShapeViewModel)shape);
        public void Insert(int index, ISceneShape shape) => _inner.Insert(index, (ShapeViewModel)shape);
        public void RemoveAt(int index) => _inner.RemoveAt(index);
    }
}
