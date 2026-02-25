using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using GraphicEditor.Common.Interfaces;
using GraphicEditor.Common.Models;
using GraphicEditor.TeamCore;
using GraphicEditor.TeamImport;

namespace GraphicEditor.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
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

    // ───── Коллекция фигур (UI-слой) ─────
    public ObservableCollection<ShapeViewModel> Shapes { get; } = new();

    // ───── Слои (UI-слой) ─────
    public ObservableCollection<LayerViewModel> Layers { get; } = new();

    // ───── Бекенд-менеджеры ─────
    private readonly SceneManager _scene;
    private readonly LayerManager _layerManager;

    // ───── Сервис создания фигур (UI-фасад над фабриками) ─────
    private readonly ShapeCreationService _shapeCreator = new();

    // ───── Буфер обмена (копирование/вставка) ─────
    private ShapeDto? _clipboard;

    // ───── Привязка к сетке ─────
    private bool _snapEnabled;
    public bool SnapEnabled { get => _snapEnabled; set => SetField(ref _snapEnabled, value); }

    // ───── Активный слой ─────
    public LayerViewModel? ActiveLayer =>
        (LayerViewModel?)_layerManager.ActiveLayer;

    public void SetActiveLayer(LayerViewModel layer)
    {
        _layerManager.SetActive(layer);
        OnPropertyChanged(nameof(ActiveLayer));
    }

    // ───── Выделенная фигура ─────
    private ShapeViewModel? _selectedShape;
    public ShapeViewModel? SelectedShape
    {
        get => _selectedShape;
        set
        {
            if (_selectedShape != null) _selectedShape.IsSelected = false;
            SetField(ref _selectedShape, value);
            if (_selectedShape != null) _selectedShape.IsSelected = true;
            OnPropertyChanged(nameof(HasSelection));
            ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
        }
    }

    public bool HasSelection => _selectedShape != null;

    // ───── Инструмент ─────
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
            StatusTool = value switch
            {
                ToolType.Select => "Выбор",
                ToolType.Circle => "Круг",
                ToolType.Rectangle => "Прямоугольник",
                ToolType.Triangle => "Треугольник",
                ToolType.Line => "Линия",
                _ => "Выбор",
            };
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

    // ───── Счётчик имён слоёв ─────
    private int _layerCount = 1;

    // ───── Undo/Redo команды ─────
    public bool CanUndo => _scene.CanUndo;
    public bool CanRedo => _scene.CanRedo;

    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand AddLayerCommand { get; }
    public ICommand DeleteLayerCommand { get; }

    // ───── Активные цвета для новых фигур ─────
    private Color _activeFillColor = Color.FromRgb(100, 149, 237);
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

    // ───── Зум ─────
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

    // ───── Статусная строка ─────
    private string _statusTool = "Выбор";
    public string StatusTool { get => _statusTool; set => SetField(ref _statusTool, value); }

    private string _statusMouse = "X: 0, Y: 0";
    public string StatusMouse { get => _statusMouse; set => SetField(ref _statusMouse, value); }

    public string StatusZoom => $"{(int)(_zoomFactor * 100)}%";

    public string StatusCanvasSize => "1600 × 1200";

    public MainWindowViewModel()
    {
        var shapesAdapter = new ShapeCollectionAdapter(Shapes);
        _scene = new SceneManager(shapesAdapter);
        _layerManager = new LayerManager(Shapes);

        Shapes.CollectionChanged += OnShapesChanged;
        _layerManager.LayerCreated += OnLayerCreated;

        UndoCommand = new RelayCommand(Undo, () => _scene.CanUndo);
        RedoCommand = new RelayCommand(Redo, () => _scene.CanRedo);
        DeleteCommand = new RelayCommand(DeleteSelected, () => _selectedShape != null);
        ClearAllCommand = new RelayCommand(ClearAll);
        AddLayerCommand = new RelayCommand(AddLayer);
        DeleteLayerCommand = new RelayCommand(DeleteLayer, () => Layers.Count > 1);

        var defaultLayer = new LayerViewModel { Name = "Слой 1" };
        RegisterLayer(defaultLayer);
        _layerManager.SetActive(defaultLayer);
        OnPropertyChanged(nameof(ActiveLayer));
    }

    // ───── Создание фигуры: делегируется ShapeCreationService ─────
    public ShapeViewModel CreateShape(ToolType tool, Point p1, Point p2) =>
        _shapeCreator.Create(
            tool, p1, p2,
            _activeFillColor, _activeStrokeColor,
            ActiveLayer?.Name ?? "Слой 1",
            ActiveLayer?.IsVisible ?? true);

    // ───── Делегирование операций в SceneManager ─────

    public void AddShape(ShapeViewModel shape) =>
        ExecuteScene(() => _scene.Add(shape));

    public void MoveShape(ShapeViewModel shape, Point delta) =>
        ExecuteScene(() => _scene.Move(shape, delta));

    public void RotateSelected(double angle)
    {
        if (_selectedShape == null) return;
        ExecuteScene(() => _scene.Rotate(_selectedShape, angle));
    }

    public void MirrorXSelected()
    {
        _selectedShape?.MirrorX();
        NotifyUndoRedo();
    }

    public void MirrorYSelected()
    {
        _selectedShape?.MirrorY();
        NotifyUndoRedo();
    }

    public void ApplyFillColor(Color color)
    {
        ActiveFillColor = color;
        if (_selectedShape == null) return;
        ExecuteScene(() => _scene.ChangeStyle(_selectedShape, color, _selectedShape.StrokeColor));
    }

    public void ApplyStrokeColor(Color color)
    {
        ActiveStrokeColor = color;
        if (_selectedShape == null) return;
        ExecuteScene(() => _scene.ChangeStyle(_selectedShape, _selectedShape.FillColor, color));
    }

    private void Undo() { _scene.Undo(); NotifyUndoRedo(); }
    private void Redo() { _scene.Redo(); NotifyUndoRedo(); }

    private void DeleteSelected()
    {
        if (_selectedShape == null) return;
        ExecuteScene(() => _scene.Delete(_selectedShape));
        SelectedShape = null;
    }

    private void ClearAll()
    {
        Shapes.Clear();
        SelectedShape = null;
        NotifyUndoRedo();
    }

    // ───── Управление слоями — делегируется LayerManager ─────

    private void AddLayer()
    {
        var layer = new LayerViewModel { Name = $"Слой {++_layerCount}" };
        RegisterLayer(layer);
        _layerManager.SetActive(layer);
        OnPropertyChanged(nameof(ActiveLayer));
        ((RelayCommand)DeleteLayerCommand).RaiseCanExecuteChanged();
    }

    private void DeleteLayer()
    {
        var toDelete = ActiveLayer;
        if (toDelete == null) return;

        if (_selectedShape?.LayerName == toDelete.Name)
            SelectedShape = null;

        foreach (var shape in toDelete.Shapes.ToList())
            Shapes.Remove(shape);

        if (_layerManager.Unregister(toDelete))
        {
            Layers.Remove(toDelete);
            _layerManager.SetActive(_layerManager.Layers[^1]);
            OnPropertyChanged(nameof(ActiveLayer));
            ((RelayCommand)DeleteLayerCommand).RaiseCanExecuteChanged();
        }
    }

    public void MoveSelectedShapeToLayer(LayerViewModel target)
    {
        if (_selectedShape == null) return;
        var fromLayer = Layers.FirstOrDefault(l => l.Name == _selectedShape.LayerName);
        fromLayer?.Shapes.Remove(_selectedShape);
        _layerManager.MoveShapeToLayer(_selectedShape, target);
        target.Shapes.Add(_selectedShape);
    }

    // ───── Синхронизация: фигуры ↔ слои (UI-логика) ─────

    private void OnShapesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null) break;
                foreach (ShapeViewModel shape in e.NewItems)
                    GetOrCreateLayerVM(shape.LayerName).Shapes.Add(shape);
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null) break;
                foreach (ShapeViewModel shape in e.OldItems)
                    foreach (var layer in Layers)
                        layer.Shapes.Remove(shape);
                break;

            case NotifyCollectionChangedAction.Reset:
                foreach (var layer in Layers)
                    layer.Shapes.Clear();
                break;
        }
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
        if (existing != null) return existing;

        var newLayer = new LayerViewModel { Name = name };
        _layerManager.GetOrCreate(name, _ => newLayer);
        return newLayer;
    }

    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayerViewModel.IsVisible) && sender is LayerViewModel layer)
            _layerManager.ApplyVisibility(layer);
    }

    // ───── Импорт ─────

    public void ImportJson(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var layer in Layers.Skip(1).ToList())
            Layers.Remove(layer);
        Layers[0].Shapes.Clear();
        _layerManager.SetActive(Layers[0]);
        OnPropertyChanged(nameof(ActiveLayer));
        _layerCount = 1;

        Shapes.Clear();
        SelectedShape = null;
        _shapeCreator.ResetCounters();

        foreach (var vm in SceneSerializer.ImportJson(path))
            Shapes.Add(vm);
    }

    // ───── Копирование / вставка ─────

    public void CopySelected()
    {
        if (_selectedShape == null) return;
        _clipboard = ShapeDto.FromViewModel(_selectedShape);
    }

    public void PasteClipboard()
    {
        if (_clipboard == null) return;
        var clone = _clipboard.ToViewModel();
        if (clone == null) return;

        clone.Name = _clipboard.Name + " (копия)";
        clone.Move(new Point(20, 20));
        clone.LayerName = ActiveLayer?.Name ?? "Слой 1";
        clone.IsVisible = ActiveLayer?.IsVisible ?? true;

        AddShape(clone);
        SelectedShape = clone;

        // Обновляем буфер, чтобы следующая вставка была со смещением
        _clipboard = ShapeDto.FromViewModel(clone);
    }

    private void ExecuteScene(System.Action action)
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
}
