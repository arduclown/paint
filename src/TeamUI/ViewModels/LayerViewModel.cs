using System.Collections.ObjectModel;
using GraphicEditor.Common.Interfaces;

namespace GraphicEditor.ViewModels;

public class LayerViewModel : ObservableBase, ILayer
{
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

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public ObservableCollection<ShapeViewModel> Shapes { get; } = [];
}
