using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common;
using GraphicEditor.Common.Interfaces;
using GraphicEditor.Common.Models;
using GraphicEditor.TeamCore;
using GraphicEditor.TeamImport;

namespace GraphicEditor.ViewModels;

public class MainWindowViewModel : ObservableBase
{
    public ObservableCollection<ShapeViewModel> Shapes { get; } = [];
    public ObservableCollection<ShapeViewModel> SelectedShapes { get; } = [];
    public ObservableCollection<LayerViewModel> Layers { get; } = [];
    public ObservableCollection<Color> RecentColors { get; } = [];

    private readonly SceneManager _scene;
    private readonly LayerManager _layerManager;
    private readonly ShapeCreationService _shapeCreator = new();
    private ShapeDto? _clipboard;

    private bool _snapEnabled;
    public bool SnapEnabled { get => _snapEnabled; set => SetField(ref _snapEnabled, value); }

    private int _gridStep = 40;
    public int GridStep { get => _gridStep; set => SetField(ref _gridStep, value); }

    public LayerViewModel? ActiveLayer => (LayerViewModel?)_layerManager.ActiveLayer;

    public bool IsActiveLayerLocked => ActiveLayer?.IsLocked == true;

    public bool IsShapeOnLockedLayer(ShapeViewModel? shape) =>
        shape is not null && Layers.FirstOrDefault(l => l.Name == shape.LayerName)?.IsLocked == true;

    public void SetActiveLayer(LayerViewModel layer)
    {
        _layerManager.SetActive(layer);
        OnPropertyChanged(nameof(ActiveLayer));
        OnPropertyChanged(nameof(StatusInfo));
    }

    private ShapeViewModel? _selectedShape;
    public ShapeViewModel? SelectedShape
    {
        get => _selectedShape;
        set
        {
            // Очищаем мульти-выделение
            ClearSelection();
            SetField(ref _selectedShape, value);
            if (_selectedShape is not null)
            {
                _selectedShape.IsSelected = true;
                SelectedShapes.Add(_selectedShape);
            }
            NotifySelectionChanged();
        }
    }

    public bool HasSelection => SelectedShapes.Count > 0;

    public void ClearSelection()
    {
        foreach (var s in SelectedShapes)
            s.IsSelected = false;
        SelectedShapes.Clear();
        _selectedShape = null;
    }

    public void AddToSelection(ShapeViewModel shape)
    {
        if (SelectedShapes.Contains(shape)) return;
        shape.IsSelected = true;
        SelectedShapes.Add(shape);
        _selectedShape = SelectedShapes.Count == 1 ? SelectedShapes[0] : null;
        NotifySelectionChanged();
    }

    public void ToggleSelection(ShapeViewModel shape)
    {
        if (SelectedShapes.Contains(shape))
        {
            shape.IsSelected = false;
            SelectedShapes.Remove(shape);
        }
        else
        {
            shape.IsSelected = true;
            SelectedShapes.Add(shape);
        }
        _selectedShape = SelectedShapes.Count == 1 ? SelectedShapes[0] : null;
        NotifySelectionChanged();
    }

    public void SelectShapesInRect(Rect rect)
    {
        ClearSelection();
        foreach (var shape in Shapes)
        {
            if (!shape.IsVisible) continue;
            var b = shape.Bounds;
            if (rect.Intersects(b))
            {
                shape.IsSelected = true;
                SelectedShapes.Add(shape);
            }
        }
        _selectedShape = SelectedShapes.Count == 1 ? SelectedShapes[0] : null;
        NotifySelectionChanged();
    }

    public Rect? GetSelectionBounds()
    {
        if (SelectedShapes.Count == 0) return null;
        var first = SelectedShapes[0].Bounds;
        double l = first.Left, t = first.Top, r = first.Right, b = first.Bottom;
        for (int i = 1; i < SelectedShapes.Count; i++)
        {
            var sb = SelectedShapes[i].Bounds;
            l = Math.Min(l, sb.Left);
            t = Math.Min(t, sb.Top);
            r = Math.Max(r, sb.Right);
            b = Math.Max(b, sb.Bottom);
        }
        return new Rect(l, t, r - l, b - t);
    }

    public void MoveSelected(Point delta)
    {
        foreach (var shape in SelectedShapes)
        {
            if (!IsShapeOnLockedLayer(shape))
                shape.Move(delta);
        }
    }

    // Прокси-свойства для привязки слайдеров
    public double SelectedOpacity
    {
        get => _selectedShape?.Opacity ?? (SelectedShapes.Count > 0 ? SelectedShapes[0].Opacity : 1.0);
        set
        {
            foreach (var s in SelectedShapes)
                s.Opacity = value;
        }
    }

    public double SelectedStrokeWidth
    {
        get => _selectedShape?.StrokeWidth ?? (SelectedShapes.Count > 0 ? SelectedShapes[0].StrokeWidth : 1.5);
        set
        {
            foreach (var s in SelectedShapes)
                s.StrokeWidth = value;
        }
    }

    private ToolType _currentTool = ToolType.Select;
    public ToolType CurrentTool
    {
        get => _currentTool;
        set
        {
            SetField(ref _currentTool, value);
            OnPropertyChanged(nameof(IsSelectTool));
            OnPropertyChanged(nameof(IsCircleTool));
            OnPropertyChanged(nameof(IsRectangleTool));
            OnPropertyChanged(nameof(IsTriangleTool));
            OnPropertyChanged(nameof(IsLineTool));
            StatusTool = ShapeRegistry.GetDisplayName(value);
        }
    }

    public bool IsSelectTool
    {
        get => CurrentTool == ToolType.Select;
        set { if (value) CurrentTool = ToolType.Select; }
    }
    public bool IsCircleTool
    {
        get => CurrentTool == ToolType.Circle;
        set { if (value) CurrentTool = ToolType.Circle; }
    }
    public bool IsRectangleTool
    {
        get => CurrentTool == ToolType.Rectangle;
        set { if (value) CurrentTool = ToolType.Rectangle; }
    }
    public bool IsTriangleTool
    {
        get => CurrentTool == ToolType.Triangle;
        set { if (value) CurrentTool = ToolType.Triangle; }
    }
    public bool IsLineTool
    {
        get => CurrentTool == ToolType.Line;
        set { if (value) CurrentTool = ToolType.Line; }
    }

    private int _nextLayerNumber = 1;

    public bool CanUndo => _scene.CanUndo;
    public bool CanRedo => _scene.CanRedo;

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand AddLayerCommand { get; }
    public ICommand DeleteLayerCommand { get; }

    // Команды трансформации
    public ICommand ScaleHalfCommand { get; }
    public ICommand ScaleDoubleCommand { get; }
    public ICommand MirrorXCommand { get; }
    public ICommand MirrorYCommand { get; }
    public ICommand ApplyAngleCommand { get; }

    // Z-порядок
    public ICommand BringToFrontCommand { get; }
    public ICommand SendToBackCommand { get; }
    public ICommand BringForwardCommand { get; }
    public ICommand SendBackwardCommand { get; }

    private string _angleInput = "";
    public string AngleInput
    {
        get => _angleInput;
        set => SetField(ref _angleInput, value);
    }

    private Color _activeFillColor = EditorConstants.DefaultFillColor;
    public Color ActiveFillColor
    {
        get => _activeFillColor;
        set => SetField(ref _activeFillColor, value);
    }

    private Color _activeStrokeColor = Colors.Black;
    public Color ActiveStrokeColor
    {
        get => _activeStrokeColor;
        set => SetField(ref _activeStrokeColor, value);
    }

    private double _zoomFactor = 1.0;
    public double ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            if (SetField(ref _zoomFactor, Math.Clamp(value, 0.25, 4.0)))
                OnPropertyChanged(nameof(StatusZoom));
        }
    }

    private string _statusTool = "Выбор";
    public string StatusTool { get => _statusTool; set => SetField(ref _statusTool, value); }

    private string _statusMouse = "X: 0, Y: 0";
    public string StatusMouse { get => _statusMouse; set => SetField(ref _statusMouse, value); }

    public string StatusZoom => $"{(int)(_zoomFactor * 100)}%";
    public string StatusCanvasSize => $"{(int)EditorConstants.CanvasWidth} x {(int)EditorConstants.CanvasHeight}";

    public string StatusInfo => $"Фигур: {Shapes.Count} | Слой: {ActiveLayer?.Name ?? "—"}";

    public MainWindowViewModel()
    {
        var shapesAdapter = new ShapeCollectionAdapter(Shapes);
        _scene = new SceneManager(shapesAdapter);
        _layerManager = new LayerManager(Shapes);

        Shapes.CollectionChanged += OnShapesChanged;
        _layerManager.LayerCreated += OnLayerCreated;

        UndoCommand = new RelayCommand(Undo, () => _scene.CanUndo);
        RedoCommand = new RelayCommand(Redo, () => _scene.CanRedo);
        DeleteCommand = new RelayCommand(DeleteSelected, () => HasSelection);
        ClearAllCommand = new RelayCommand(ClearAll);
        AddLayerCommand = new RelayCommand(AddLayer);
        DeleteLayerCommand = new RelayCommand(DeleteLayer, () => Layers.Count > 1);

        ScaleHalfCommand = new RelayCommand(() => _selectedShape?.Scale(0.5), () => HasSelection);
        ScaleDoubleCommand = new RelayCommand(() => _selectedShape?.Scale(2.0), () => HasSelection);
        MirrorXCommand = new RelayCommand(MirrorXSelected, () => HasSelection);
        MirrorYCommand = new RelayCommand(MirrorYSelected, () => HasSelection);
        ApplyAngleCommand = new RelayCommand(ApplyAngle, () => HasSelection);

        BringToFrontCommand = new RelayCommand(BringToFront, () => HasSelection);
        SendToBackCommand = new RelayCommand(SendToBack, () => HasSelection);
        BringForwardCommand = new RelayCommand(BringForward, () => HasSelection);
        SendBackwardCommand = new RelayCommand(SendBackward, () => HasSelection);

        var defaultLayer = new LayerViewModel { Name = EditorConstants.DefaultLayerName };
        RegisterLayer(defaultLayer);
        _layerManager.SetActive(defaultLayer);
        OnPropertyChanged(nameof(ActiveLayer));
    }

    public Point SnapToGrid(Point p)
    {
        double step = GridStep;
        return new Point(
            Math.Round(p.X / step, MidpointRounding.AwayFromZero) * step,
            Math.Round(p.Y / step, MidpointRounding.AwayFromZero) * step);
    }

    public ShapeViewModel CreateShape(ToolType tool, Point p1, Point p2) =>
        _shapeCreator.Create(
            tool, p1, p2,
            _activeFillColor, _activeStrokeColor,
            ActiveLayer?.Name ?? EditorConstants.DefaultLayerName,
            ActiveLayer?.IsVisible ?? true);

    public void AddShape(ShapeViewModel shape)
    {
        if (IsActiveLayerLocked) return;
        ExecuteScene(() => _scene.Add(shape));
    }

    public void RotateSelected(double angle)
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        ExecuteScene(() => _scene.Rotate(_selectedShape, angle));
    }

    private void MirrorXSelected()
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        _selectedShape.MirrorX();
        NotifyUndoRedo();
    }

    private void MirrorYSelected()
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        _selectedShape.MirrorY();
        NotifyUndoRedo();
    }

    private void ApplyAngle()
    {
        if (double.TryParse(_angleInput, out double angle))
        {
            RotateSelected(angle);
            AngleInput = "";
        }
    }

    // Z-порядок
    private void BringToFront()
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        int idx = Shapes.IndexOf(_selectedShape);
        if (idx < 0 || idx == Shapes.Count - 1) return;
        Shapes.Move(idx, Shapes.Count - 1);
    }

    private void SendToBack()
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        int idx = Shapes.IndexOf(_selectedShape);
        if (idx <= 0) return;
        Shapes.Move(idx, 0);
    }

    private void BringForward()
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        int idx = Shapes.IndexOf(_selectedShape);
        if (idx < 0 || idx == Shapes.Count - 1) return;
        Shapes.Move(idx, idx + 1);
    }

    private void SendBackward()
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        int idx = Shapes.IndexOf(_selectedShape);
        if (idx <= 0) return;
        Shapes.Move(idx, idx - 1);
    }

    public void ApplyFillColor(Color color)
    {
        ActiveFillColor = color;
        AddRecentColor(color);
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        ExecuteScene(() => _scene.ChangeStyle(_selectedShape, color, _selectedShape.StrokeColor));
    }

    public void ApplyStrokeColor(Color color)
    {
        ActiveStrokeColor = color;
        AddRecentColor(color);
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        ExecuteScene(() => _scene.ChangeStyle(_selectedShape, _selectedShape.FillColor, color));
    }

    private void AddRecentColor(Color color)
    {
        if (color == Colors.Transparent) return;
        RecentColors.Remove(color);
        RecentColors.Insert(0, color);
        while (RecentColors.Count > 8) RecentColors.RemoveAt(RecentColors.Count - 1);
    }

    private void Undo() { _scene.Undo(); NotifyUndoRedo(); }
    private void Redo() { _scene.Redo(); NotifyUndoRedo(); }

    private void DeleteSelected()
    {
        if (SelectedShapes.Count == 0) return;
        var toDelete = SelectedShapes.Where(s => !IsShapeOnLockedLayer(s)).ToList();
        foreach (var shape in toDelete)
            ExecuteScene(() => _scene.Delete(shape));
        SelectedShape = null;
    }

    private void ClearAll()
    {
        if (IsActiveLayerLocked) return;
        var layerName = ActiveLayer?.Name;
        if (layerName is null) return;

        if (_selectedShape?.LayerName == layerName)
            SelectedShape = null;

        var toRemove = Shapes.Where(s => s.LayerName == layerName).ToList();
        foreach (var shape in toRemove)
            Shapes.Remove(shape);

        NotifyUndoRedo();
    }

    private void AddLayer()
    {
        var layer = new LayerViewModel { Name = $"Слой {++_nextLayerNumber}" };
        RegisterLayer(layer);
        _layerManager.SetActive(layer);
        OnPropertyChanged(nameof(ActiveLayer));
        OnPropertyChanged(nameof(StatusInfo));
        ((RelayCommand)DeleteLayerCommand).RaiseCanExecuteChanged();
    }

    private void DeleteLayer()
    {
        var toDelete = ActiveLayer;
        if (toDelete is null) return;

        if (_selectedShape?.LayerName == toDelete.Name)
            SelectedShape = null;

        foreach (var shape in toDelete.Shapes.ToList())
            Shapes.Remove(shape);

        if (_layerManager.Unregister(toDelete))
        {
            Layers.Remove(toDelete);
            _layerManager.SetActive(_layerManager.Layers[^1]);
            OnPropertyChanged(nameof(ActiveLayer));
            OnPropertyChanged(nameof(StatusInfo));
            ((RelayCommand)DeleteLayerCommand).RaiseCanExecuteChanged();
        }
    }

    public void MoveSelectedShapeToLayer(LayerViewModel target)
    {
        if (_selectedShape is null || IsShapeOnLockedLayer(_selectedShape)) return;
        if (target.IsLocked) return;
        var fromLayer = Layers.FirstOrDefault(l => l.Name == _selectedShape.LayerName);
        fromLayer?.Shapes.Remove(_selectedShape);
        _layerManager.MoveShapeToLayer(_selectedShape, target);
        target.Shapes.Add(_selectedShape);
    }

    private void OnShapesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is null) break;
                foreach (ShapeViewModel shape in e.NewItems)
                    GetOrCreateLayerVM(shape.LayerName).Shapes.Add(shape);
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is null) break;
                foreach (ShapeViewModel shape in e.OldItems)
                    foreach (var layer in Layers)
                        layer.Shapes.Remove(shape);
                break;

            case NotifyCollectionChangedAction.Reset:
                foreach (var layer in Layers)
                    layer.Shapes.Clear();
                break;
        }

        OnPropertyChanged(nameof(StatusInfo));
    }

    private void RegisterLayer(LayerViewModel layer)
    {
        layer.PropertyChanged += Layer_PropertyChanged;
        _layerManager.Register(layer);
        Layers.Add(layer);
    }

    private void OnLayerCreated(ILayer layer)
    {
        var lvm = (LayerViewModel)layer;
        lvm.PropertyChanged += Layer_PropertyChanged;
        Layers.Add(lvm);
        ((RelayCommand)DeleteLayerCommand).RaiseCanExecuteChanged();
    }

    private LayerViewModel GetOrCreateLayerVM(string name)
    {
        var existing = Layers.FirstOrDefault(l => l.Name == name);
        if (existing is not null) return existing;

        var newLayer = new LayerViewModel { Name = name };
        _layerManager.GetOrCreate(name, _ => newLayer);
        return newLayer;
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayerViewModel.IsVisible) && sender is LayerViewModel layer)
            _layerManager.ApplyVisibility(layer);
    }

    public void ImportJson(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var layer in Layers.Skip(1).ToList())
            Layers.Remove(layer);
        Layers[0].Shapes.Clear();
        _layerManager.SetActive(Layers[0]);
        OnPropertyChanged(nameof(ActiveLayer));
        _nextLayerNumber = 1;

        Shapes.Clear();
        SelectedShape = null;
        _shapeCreator.ResetCounters();

        foreach (var vm in SceneSerializer.ImportJson(path))
            Shapes.Add(vm);
    }

    public void CopySelected()
    {
        if (_selectedShape is null) return;
        _clipboard = ShapeDto.FromViewModel(_selectedShape);
    }

    public void PasteClipboard()
    {
        if (_clipboard is null) return;
        var clone = _clipboard.ToViewModel();
        if (clone is null) return;

        clone.Name = $"{_clipboard.Name} (копия)";
        clone.Move(new Point(20, 20));
        clone.LayerName = ActiveLayer?.Name ?? EditorConstants.DefaultLayerName;
        clone.IsVisible = ActiveLayer?.IsVisible ?? true;

        AddShape(clone);
        SelectedShape = clone;

        _clipboard = ShapeDto.FromViewModel(clone);
    }

    private void ExecuteScene(Action action)
    {
        action();
        NotifyUndoRedo();
    }

    private void NotifyUndoRedo()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        ((RelayCommand)UndoCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RedoCommand).RaiseCanExecuteChanged();
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedShape));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedOpacity));
        OnPropertyChanged(nameof(SelectedStrokeWidth));
        RaiseAllCommands();
    }

    private void RaiseAllCommands()
    {
        ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ScaleHalfCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ScaleDoubleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MirrorXCommand).RaiseCanExecuteChanged();
        ((RelayCommand)MirrorYCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ApplyAngleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BringToFrontCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SendToBackCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BringForwardCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SendBackwardCommand).RaiseCanExecuteChanged();
    }
}
