using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.ViewModels;

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

    private string _name = "Фигура";
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

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

    private Color _fillColor = Color.FromRgb(100, 149, 237);
    public Color FillColor
    {
        get => _fillColor;
        set => SetField(ref _fillColor, value);
    }

    private Color _strokeColor = Colors.Black;
    public Color StrokeColor
    {
        get => _strokeColor;
        set => SetField(ref _strokeColor, value);
    }

    private double _opacity = 1.0;
    public double Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, value);
    }

    // Толщина обводки, задаваемая пользователем
    private double _strokeWidth = 1.5;
    public double StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            if (SetField(ref _strokeWidth, Math.Clamp(value, 0.5, 10.0)))
                OnPropertyChanged(nameof(StrokeThickness));
        }
    }

    /// <summary>Итоговая толщина: выделенная фигура — не тоньше 3.0.</summary>
    public double StrokeThickness => IsSelected ? Math.Max(StrokeWidth, 3.0) : StrokeWidth;

    // Кумулятивный угол поворота (для отображения в панели свойств)
    private double _rotationAngle;
    public double RotationAngle
    {
        get => _rotationAngle;
        set => SetField(ref _rotationAngle, value);
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    private string _layerName = "Слой 1";
    public string LayerName
    {
        get => _layerName;
        set => SetField(ref _layerName, value);
    }

    public abstract string Geometry { get; }
    public abstract Rect Bounds { get; }
    public abstract string ShapeType { get; }

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
