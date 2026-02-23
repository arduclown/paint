using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.ViewModels
{
    // Представление слоя для UI. Реализует ILayer — интерфейс бекенда.
    public class LayerViewModel : INotifyPropertyChanged, ILayer
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private string _name = "Слой";
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set => SetField(ref _isLocked, value);
        }

        // Активен ли слой (выбран как текущий)
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetField(ref _isActive, value);
        }

        // Фигуры, принадлежащие этому слою
        public ObservableCollection<ShapeViewModel> Shapes { get; } = new();
    }
}
