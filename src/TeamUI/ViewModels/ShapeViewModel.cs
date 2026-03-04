using System;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.ViewModels;

public abstract class ShapeViewModel : ObservableBase, ISceneShape
{
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

    private Color _fillColor = EditorConstants.DefaultFillColor;
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

    public double StrokeThickness => IsSelected ? Math.Max(StrokeWidth, 3.0) : StrokeWidth;

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

    private string _layerName = EditorConstants.DefaultLayerName;
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
    public abstract void Scale(double ratioX, double ratioY);
    public abstract void Rotate(double angle);
    public abstract void MirrorX();
    public abstract void MirrorY();

    protected void NotifyGeometryChanged()
    {
        OnPropertyChanged(nameof(Geometry));
        OnPropertyChanged(nameof(Bounds));
    }
}
