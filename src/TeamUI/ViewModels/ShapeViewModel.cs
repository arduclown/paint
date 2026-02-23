using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.TeamCore.Scene;

namespace GraphicEditor.ViewModels
{
    public abstract class ShapeViewModel : INotifyPropertyChanged, ISceneShape
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        // Имя фигуры (отображается в списке)
        private string _name = "Фигура";
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        // Выделена ли фигура
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                SetField(ref _isSelected, value);
                OnPropertyChanged(nameof(StrokeThickness));
            }
        }

        // Цвет заливки
        private Color _fillColor = Color.FromRgb(100, 149, 237);
        public Color FillColor
        {
            get => _fillColor;
            set => SetField(ref _fillColor, value);
        }

        // Цвет обводки
        private Color _strokeColor = Colors.Black;
        public Color StrokeColor
        {
            get => _strokeColor;
            set => SetField(ref _strokeColor, value);
        }

        // Прозрачность (0.0 — 1.0)
        private double _opacity = 1.0;
        public double Opacity
        {
            get => _opacity;
            set => SetField(ref _opacity, value);
        }

        // Толщина обводки: увеличивается при выделении
        public double StrokeThickness => IsSelected ? 3.0 : 1.5;

        // Видимость фигуры
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetField(ref _isVisible, value);
        }

        // Слой, которому принадлежит фигура
        private string _layerName = "Слой 1";
        public string LayerName
        {
            get => _layerName;
            set => SetField(ref _layerName, value);
        }

        // SVG-путь для отрисовки в Avalonia Path.Data
        public abstract string Geometry { get; }

        // Ограничивающий прямоугольник (для маркеров масштаба)
        public abstract Rect Bounds { get; }

        // Тип фигуры для сериализации
        public abstract string ShapeType { get; }

        // Трансформации, делегируются модели
        public abstract void Move(Point delta);
        public abstract void Scale(double ratio);
        public abstract void Rotate(double angle);
        public abstract void MirrorX();
        public abstract void MirrorY();

        protected void NotifyGeometryChanged()
        {
            OnPropertyChanged(nameof(Geometry));
            OnPropertyChanged(nameof(Bounds));
        }
    }
}
