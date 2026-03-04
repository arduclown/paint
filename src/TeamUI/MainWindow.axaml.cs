using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using GraphicEditor.Common.Models;
using GraphicEditor.TeamImport;
using GraphicEditor.ViewModels;

namespace GraphicEditor;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;
    private MainWindowViewModel VM => _viewModel ?? (DataContext as MainWindowViewModel)!;

    private bool _isDrawing;
    private Point _drawStart;

    private bool _isDragging;
    private ShapeViewModel? _dragTarget;
    private Point _dragLastPos;

    private bool _isResizing;
    private int _resizeHandleIndex;
    private Point _resizeCenter;
    private Point _resizeStartPos;
    private double _resizeStartDist;
    private double _resizeLastRatio;

    private bool _isRotating;
    private double _rotateStartAngle;

    private readonly Avalonia.Controls.Shapes.Rectangle[] _handles = new Avalonia.Controls.Shapes.Rectangle[4];
    private ShapeViewModel? _handleShape;

    private Ellipse? _rotationHandle;
    private Line? _rotationLine;

    // Drag-and-drop в списке фигур
    private bool _isListDragging;
    private ShapeViewModel? _listDragShape;
    private Point _listDragStart;

    // Rubber band выделение
    private bool _isRubberBand;
    private Point _rubberBandStart;

    // Перетаскивание группы
    private bool _isMultiDragging;
    private Point _multiDragLastPos;

    private static readonly (Color color, string name)[] Palette =
    [
        (Colors.CornflowerBlue,  "Голубой"),
        (Colors.Crimson,         "Красный"),
        (Colors.MediumSeaGreen,  "Зелёный"),
        (Colors.Gold,            "Жёлтый"),
        (Colors.MediumOrchid,    "Фиолетовый"),
        (Colors.Coral,           "Коралловый"),
        (Colors.Teal,            "Бирюзовый"),
        (Colors.SaddleBrown,     "Коричневый"),
        (Colors.White,           "Белый"),
        (Colors.LightGray,       "Серый"),
        (Colors.Black,           "Чёрный"),
        (Colors.Transparent,     "Без заливки"),
    ];

    public MainWindow()
    {
        DataContext = new MainWindowViewModel();
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, EventArgs e)
    {
        BuildColorPalette(FillColorPanel, isFill: true);
        BuildColorPalette(StrokeColorPanel, isFill: false);
        DrawGrid(VM.GridStep);
        InitHandles();
        SyncColorViews();
    }

    private void SyncColorViews()
    {
        if (FillColorView is not null)
            FillColorView.Color = VM.ActiveFillColor;
        if (StrokeColorView is not null)
            StrokeColorView.Color = VM.ActiveStrokeColor;
    }

    private void BuildColorPalette(WrapPanel panel, bool isFill)
    {
        foreach (var (color, name) in Palette)
        {
            var btn = new Button
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
            };
            ToolTip.SetTip(btn, name);

            if (color == Colors.Transparent)
            {
                btn.Content = new TextBlock
                {
                    Text = "✕",
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
            }

            var captured = color;
            btn.Click += (_, _) =>
            {
                if (isFill)
                {
                    VM.ApplyFillColor(captured);
                    if (FillColorView is not null) FillColorView.Color = captured;
                }
                else
                {
                    VM.ApplyStrokeColor(captured);
                    if (StrokeColorView is not null) StrokeColorView.Color = captured;
                }
            };

            panel.Children.Add(btn);
        }
    }

    private void DrawGrid(double step)
    {
        if (GridCanvas is null) return;
        GridCanvas.Children.Clear();

        const double w = 1600, h = 1200;
        var geomGroup = new GeometryGroup();

        for (double x = 0; x <= w; x += step)
            geomGroup.Children.Add(new LineGeometry(new Point(x, 0), new Point(x, h)));
        for (double y = 0; y <= h; y += step)
            geomGroup.Children.Add(new LineGeometry(new Point(0, y), new Point(w, y)));

        GridCanvas.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data = geomGroup,
            Stroke = new SolidColorBrush(Color.FromArgb(35, 150, 150, 150)),
            StrokeThickness = 0.5,
            IsHitTestVisible = false,
        });
    }

    private void InitHandles()
    {
        for (int i = 0; i < 4; i++)
        {
            var h = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = 8, Height = 8,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(86, 156, 214)),
                StrokeThickness = 1.5,
                IsVisible = false,
                Cursor = new Cursor(StandardCursorType.SizeAll),
            };
            h.PointerPressed += Handle_PointerPressed;
            _handles[i] = h;
            HandlesCanvas.Children.Add(h);
        }

        _rotationLine = new Line
        {
            Stroke = new SolidColorBrush(Color.FromRgb(80, 200, 80)),
            StrokeThickness = 1,
            IsVisible = false,
            IsHitTestVisible = false,
        };
        HandlesCanvas.Children.Add(_rotationLine);

        _rotationHandle = new Ellipse
        {
            Width = 12, Height = 12,
            Fill = new SolidColorBrush(Color.FromRgb(80, 200, 80)),
            Stroke = Brushes.White,
            StrokeThickness = 1.5,
            IsVisible = false,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        _rotationHandle.PointerPressed += RotationHandle_PointerPressed;
        HandlesCanvas.Children.Add(_rotationHandle);
    }

    private void UpdateHandles(ShapeViewModel? shape)
    {
        if (shape is null)
        {
            foreach (var h in _handles) h.IsVisible = false;
            if (_rotationHandle is not null) _rotationHandle.IsVisible = false;
            if (_rotationLine is not null) _rotationLine.IsVisible = false;
            return;
        }

        var b = shape.Bounds;
        double[] xs = [b.Left, b.Right, b.Right, b.Left];
        double[] ys = [b.Top, b.Top, b.Bottom, b.Bottom];

        for (int i = 0; i < 4; i++)
        {
            Canvas.SetLeft(_handles[i], xs[i] - 4);
            Canvas.SetTop(_handles[i], ys[i] - 4);
            _handles[i].IsVisible = true;
        }

        if (_rotationHandle is not null && _rotationLine is not null)
        {
            double cx = b.Left + b.Width / 2.0;
            double topY = b.Top;
            const double stemLen = 25;

            Canvas.SetLeft(_rotationHandle, cx - 6);
            Canvas.SetTop(_rotationHandle, topY - stemLen - 6);
            _rotationHandle.IsVisible = true;

            _rotationLine.StartPoint = new Point(cx, topY);
            _rotationLine.EndPoint = new Point(cx, topY - stemLen);
            _rotationLine.IsVisible = true;
        }
    }

    private void Handle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        if (VM.SelectedShape is null) return;
        if (VM.IsShapeOnLockedLayer(VM.SelectedShape)) return;

        _isResizing = true;
        _resizeHandleIndex = Array.IndexOf(_handles, sender);
        var b = VM.SelectedShape.Bounds;
        _resizeCenter = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
        _resizeStartPos = e.GetPosition(DrawingCanvas);
        _resizeStartDist = Math.Max(1, Dist(_resizeCenter, _resizeStartPos));
        _resizeLastRatio = 1.0;

        e.Pointer.Capture(DrawingCanvas);
        e.Handled = true;
    }

    private void RotationHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        if (VM.SelectedShape is null) return;
        if (VM.IsShapeOnLockedLayer(VM.SelectedShape)) return;

        _isRotating = true;
        var b = VM.SelectedShape.Bounds;
        var center = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
        var pos = e.GetPosition(DrawingCanvas);
        _rotateStartAngle = Math.Atan2(pos.Y - center.Y, pos.X - center.X) * 180.0 / Math.PI;

        e.Pointer.Capture(DrawingCanvas);
        e.Handled = true;
    }

    private static double Dist(Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point ApplyShiftConstraint(ToolType tool, Point start, Point current) =>
        ShapeRegistry.GetByTool(tool)?.ShiftConstraint?.Invoke(start, current) ?? current;

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        DrawingCanvas.Focus();
        var pos = e.GetPosition(DrawingCanvas);

        if (VM.CurrentTool == ToolType.Select)
        {
            // Клик по пустому месту — начинаем rubber band
            _isRubberBand = true;
            _rubberBandStart = pos;
            RubberBand.IsVisible = false;
            VM.ClearSelection();
            OnSelectionChanged();
            e.Pointer.Capture(DrawingCanvas);
            return;
        }

        if (VM.ActiveLayer?.IsLocked == true) return;
        if (VM.SnapEnabled) pos = VM.SnapToGrid(pos);

        _isDrawing = true;
        _drawStart = pos;
        PreviewPath.IsVisible = true;
        UpdatePreview(pos);
        e.Pointer.Capture(DrawingCanvas);
    }

    private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);
        VM.StatusMouse = $"X: {(int)pos.X}, Y: {(int)pos.Y}";

        if (_isRubberBand)
            HandleRubberBandMove(pos);
        else if (_isMultiDragging)
            HandleMultiDragMove(pos);
        else if (_isDrawing)
            HandleDrawingMove(pos, e.KeyModifiers);
        else if (_isRotating && VM.SelectedShape is not null)
            HandleRotatingMove(pos, e.KeyModifiers);
        else if (_isResizing && VM.SelectedShape is not null)
            HandleResizingMove(pos, e.KeyModifiers);
        else if (_isDragging && _dragTarget is not null)
            HandleSingleDragMove(pos);
    }

    private void HandleRubberBandMove(Point pos)
    {
        double x = Math.Min(_rubberBandStart.X, pos.X);
        double y = Math.Min(_rubberBandStart.Y, pos.Y);
        double w = Math.Abs(pos.X - _rubberBandStart.X);
        double h = Math.Abs(pos.Y - _rubberBandStart.Y);
        Canvas.SetLeft(RubberBand, x);
        Canvas.SetTop(RubberBand, y);
        RubberBand.Width = w;
        RubberBand.Height = h;
        RubberBand.IsVisible = w > 3 || h > 3;
    }

    private void HandleMultiDragMove(Point pos)
    {
        var delta = new Point(pos.X - _multiDragLastPos.X, pos.Y - _multiDragLastPos.Y);
        VM.MoveSelected(delta);
        _multiDragLastPos = pos;
    }

    private void HandleDrawingMove(Point pos, KeyModifiers modifiers)
    {
        if (VM.SnapEnabled) pos = VM.SnapToGrid(pos);
        if (modifiers.HasFlag(KeyModifiers.Shift))
            pos = ApplyShiftConstraint(VM.CurrentTool, _drawStart, pos);
        UpdatePreview(pos);
    }

    private void HandleRotatingMove(Point pos, KeyModifiers modifiers)
    {
        var b = VM.SelectedShape!.Bounds;
        var center = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
        double currentAngle = Math.Atan2(pos.Y - center.Y, pos.X - center.X) * 180.0 / Math.PI;
        double delta = currentAngle - _rotateStartAngle;

        if (modifiers.HasFlag(KeyModifiers.Shift))
            delta = Math.Round(delta / 15.0) * 15.0;

        if (Math.Abs(delta) > 0.1)
        {
            VM.SelectedShape!.Rotate(delta);
            _rotateStartAngle = currentAngle;
        }
    }

    private void HandleResizingMove(Point pos, KeyModifiers modifiers)
    {
        double dx0 = Math.Abs(_resizeStartPos.X - _resizeCenter.X);
        double dy0 = Math.Abs(_resizeStartPos.Y - _resizeCenter.Y);
        double dxN = Math.Abs(pos.X - _resizeCenter.X);
        double dyN = Math.Abs(pos.Y - _resizeCenter.Y);

        if (dx0 < 5) dx0 = 5;
        if (dy0 < 5) dy0 = 5;

        double ratioX = dxN / dx0;
        double ratioY = dyN / dy0;

        if (ratioX < 0.05 || ratioX > 20) ratioX = 1.0;
        if (ratioY < 0.05 || ratioY > 20) ratioY = 1.0;

        double incX = ratioX / _resizeLastRatio;
        double incY = ratioY / _resizeLastRatio;

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            double uniform = Math.Max(incX, incY);
            incX = incY = uniform;
        }

        if (Math.Abs(incX - 1.0) > 0.001 || Math.Abs(incY - 1.0) > 0.001)
        {
            VM.SelectedShape!.Scale(incX, incY);
            _resizeLastRatio = ratioX;
        }
    }

    private void HandleSingleDragMove(Point pos)
    {
        var delta = new Point(pos.X - _dragLastPos.X, pos.Y - _dragLastPos.Y);
        _dragTarget!.Move(delta);
        _dragLastPos = pos;
    }

    private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);

        if (_isRubberBand)
            HandleRubberBandRelease(pos, e);

        if (_isMultiDragging)
        {
            _isMultiDragging = false;
            e.Pointer.Capture(null);
        }

        if (_isDrawing)
            HandleDrawingRelease(pos, e);

        if (_isResizing)
        {
            _isResizing = false;
            e.Pointer.Capture(null);
        }

        if (_isRotating)
        {
            _isRotating = false;
            e.Pointer.Capture(null);
        }

        if (_isDragging && _dragTarget is not null)
        {
            _isDragging = false;
            _dragTarget = null;
            e.Pointer.Capture(null);
        }
    }

    private void HandleRubberBandRelease(Point pos, PointerReleasedEventArgs e)
    {
        _isRubberBand = false;
        RubberBand.IsVisible = false;
        e.Pointer.Capture(null);

        double x = Math.Min(_rubberBandStart.X, pos.X);
        double y = Math.Min(_rubberBandStart.Y, pos.Y);
        double w = Math.Abs(pos.X - _rubberBandStart.X);
        double h = Math.Abs(pos.Y - _rubberBandStart.Y);

        if (w > 3 || h > 3)
        {
            VM.SelectShapesInRect(new Rect(x, y, w, h));
            OnSelectionChanged();
        }
    }

    private void HandleDrawingRelease(Point pos, PointerReleasedEventArgs e)
    {
        _isDrawing = false;
        PreviewPath.IsVisible = false;
        e.Pointer.Capture(null);

        if (VM.SnapEnabled) pos = VM.SnapToGrid(pos);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            pos = ApplyShiftConstraint(VM.CurrentTool, _drawStart, pos);

        var shape = VM.CreateShape(VM.CurrentTool, _drawStart, pos);
        VM.AddShape(shape);
        VM.SelectedShape = shape;
    }

    private void Shape_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;

        if (sender is Avalonia.Controls.Shapes.Path path
            && path.DataContext is ShapeViewModel vm)
        {
            VM.CurrentTool = ToolType.Select;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                VM.ToggleSelection(vm);
                OnSelectionChanged();
            }
            else if (VM.SelectedShapes.Contains(vm) && VM.SelectedShapes.Count > 1)
            {
                // Клик по уже выделенной при мульти-выделении — начинаем перетаскивание группы
                if (!VM.IsShapeOnLockedLayer(vm))
                {
                    _isMultiDragging = true;
                    _multiDragLastPos = e.GetPosition(DrawingCanvas);
                    e.Pointer.Capture(DrawingCanvas);
                }
            }
            else
            {
                VM.SelectedShape = vm;
                OnSelectionChanged();

                if (!VM.IsShapeOnLockedLayer(vm))
                {
                    _isDragging = true;
                    _dragTarget = vm;
                    _dragLastPos = e.GetPosition(DrawingCanvas);
                    e.Pointer.Capture(DrawingCanvas);
                }
            }
            e.Handled = true;
        }
    }

    private void UpdatePreview(Point current)
    {
        var pathData = ShapeRegistry.GetByTool(VM.CurrentTool)?.BuildPreview?.Invoke(_drawStart, current);

        if (pathData is not null)
        {
            try { PreviewPath.Data = Geometry.Parse(pathData); }
            catch { PreviewPath.Data = null; }
        }
    }

    private void ShapeNameBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && VM.SelectedShape is not null
            && tb.Text != VM.SelectedShape.Name)
        {
            VM.SelectedShape.Name = tb.Text ?? "";
        }
    }

    private void AngleBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            VM.ApplyAngleCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void FillColorView_ColorChanged(object? sender, Avalonia.Controls.ColorChangedEventArgs e)
    {
        VM.ApplyFillColor(e.NewColor);
    }

    private void StrokeColorView_ColorChanged(object? sender, Avalonia.Controls.ColorChangedEventArgs e)
    {
        VM.ApplyStrokeColor(e.NewColor);
    }

    // Drag-and-drop в списке фигур
    private void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is ShapeViewModel shape)
        {
            _listDragShape = shape;
            _listDragStart = e.GetPosition(this);
            _isListDragging = false;
            e.Handled = true;
        }
    }

    private void DragHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_listDragShape is null) return;
        var pos = e.GetPosition(this);
        var delta = pos - _listDragStart;

        if (!_isListDragging && (Math.Abs(delta.X) > 5 || Math.Abs(delta.Y) > 5))
            _isListDragging = true;

        if (!_isListDragging) return;

        // Определяем целевой индекс
        var listBox = this.FindControl<ListBox>("ShapesList")
            ?? this.GetVisualDescendants().OfType<ListBox>().FirstOrDefault(lb => lb.ItemsSource == VM.Shapes);
        if (listBox is null) return;

        int fromIdx = VM.Shapes.IndexOf(_listDragShape);
        int targetIdx = fromIdx;

        // Ищем элемент под курсором
        foreach (var item in listBox.GetVisualDescendants().OfType<ListBoxItem>())
        {
            var itemBounds = item.Bounds;
            var itemPos = item.TranslatePoint(new Point(0, 0), this);
            if (itemPos is null) continue;
            double midY = itemPos.Value.Y + itemBounds.Height / 2;
            if (pos.Y < midY && item.DataContext is ShapeViewModel sv)
            {
                targetIdx = VM.Shapes.IndexOf(sv);
                break;
            }
            if (item.DataContext is ShapeViewModel sv2)
                targetIdx = VM.Shapes.IndexOf(sv2) + 1;
        }

        if (targetIdx != fromIdx && targetIdx >= 0 && targetIdx <= VM.Shapes.Count)
        {
            if (targetIdx > fromIdx) targetIdx--;
            if (targetIdx != fromIdx)
                VM.Shapes.Move(fromIdx, targetIdx);
        }
    }

    private void DragHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _listDragShape = null;
        _isListDragging = false;
    }

    private void GridStepCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Content?.ToString(), out int step))
        {
            vm.GridStep = step;
            DrawGrid(step);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (e is not null && DataContext is MainWindowViewModel newVm)
        {
            // Unsubscribe old if any
            if (_viewModel is not null)
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel = newVm;
            newVm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private async void ImportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Импортировать сцену",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON") { Patterns = ["*.json"] },
            ]
        });

        if (files.Count == 0) return;

        try
        {
            VM.ImportJson(files[0].Path.LocalPath);
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Ошибка импорта", ex.Message);
        }
    }

    private async void ExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспортировать сцену",
            SuggestedFileName = "scene",
            FileTypeChoices =
            [
                new FilePickerFileType("SVG") { Patterns = ["*.svg"] },
                new FilePickerFileType("PDF") { Patterns = ["*.pdf"] },
                new FilePickerFileType("JSON") { Patterns = ["*.json"] },
            ]
        });

        if (file is null) return;

        try
        {
            var path = file.Path.LocalPath;
            switch (System.IO.Path.GetExtension(path).ToLowerInvariant())
            {
                case ".svg":  SvgExporter.Export(VM.Shapes, path); break;
                case ".pdf":  PdfExporter.Export(VM.Shapes, path); break;
                case ".json": SceneSerializer.ExportJson(VM.Shapes, path); break;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Ошибка экспорта", ex.Message);
        }
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dlg = new Window
        {
            Title = title,
            Width = 420, Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.OrangeRed,
            }
        };
        await dlg.ShowDialog(this);
    }

    private void LayerShape_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        if (sender is Avalonia.Controls.Border border && border.DataContext is ShapeViewModel shape)
        {
            VM.SelectedShape = shape;
            e.Handled = true;
        }
    }

    private void LayerName_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Avalonia.Controls.TextBlock tb && tb.DataContext is LayerViewModel layer)
            VM.SetActiveLayer(layer);
    }

    private bool _suppressLayerComboChange;

    private void LayerComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressLayerComboChange) return;
        if (LayerComboBox.SelectedItem is LayerViewModel target)
            VM.MoveSelectedShapeToLayer(target);
    }

    private void ViewModel_PropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedShape))
        {
            OnSelectionChanged();
        }
    }

    private void OnHandleShapeChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShapeViewModel.Bounds))
            UpdateSelectionVisuals();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var focused = FocusManager?.GetFocusedElement();
        if (focused is TextBox or ComboBox) return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    ExportButton_Click(this, new Avalonia.Interactivity.RoutedEventArgs());
                    e.Handled = true;
                    return;
                case Key.O:
                    ImportButton_Click(this, new Avalonia.Interactivity.RoutedEventArgs());
                    e.Handled = true;
                    return;
                case Key.C:
                    VM.CopySelected();
                    e.Handled = true;
                    return;
                case Key.V:
                    VM.PasteClipboard();
                    e.Handled = true;
                    return;
                case Key.D:
                    VM.CopySelected();
                    VM.PasteClipboard();
                    e.Handled = true;
                    return;
            }
        }

        if (e.KeyModifiers != KeyModifiers.None) return;

        if (e.Key == Key.S)
        {
            VM.CurrentTool = ToolType.Select;
            e.Handled = true;
        }
        else
        {
            var tool = ShapeRegistry.GetByHotkey(e.Key);
            if (tool.HasValue)
            {
                VM.CurrentTool = tool.Value;
                e.Handled = true;
            }
        }
    }

    private void Canvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        VM.ZoomFactor += e.Delta.Y > 0 ? 0.1 : -0.1;
        e.Handled = true;
    }

    private void UpdateSelectionVisuals()
    {
        var bounds = VM.GetSelectionBounds();
        if (bounds is null)
        {
            SelectionRect.IsVisible = false;
            UpdateHandles(null);
            return;
        }

        var b = bounds.Value;
        const double pad = 4;
        Canvas.SetLeft(SelectionRect, b.Left - pad);
        Canvas.SetTop(SelectionRect, b.Top - pad);
        SelectionRect.Width = b.Width + pad * 2;
        SelectionRect.Height = b.Height + pad * 2;
        SelectionRect.IsVisible = true;

        // Показываем хэндлы только для одиночного выделения
        if (VM.SelectedShapes.Count == 1)
            UpdateHandles(VM.SelectedShapes[0]);
        else
            UpdateHandles(null);
    }

    private void OnSelectionChanged()
    {
        var shape = VM.SelectedShape;
        ShapeNameBox.Text = shape?.Name ?? "";

        _suppressLayerComboChange = true;
        LayerComboBox.SelectedItem = shape is not null
            ? VM.Layers.FirstOrDefault(l => l.Name == shape.LayerName)
            : null;
        _suppressLayerComboChange = false;

        if (_handleShape is not null)
            _handleShape.PropertyChanged -= OnHandleShapeChanged;
        _handleShape = shape;
        if (_handleShape is not null)
            _handleShape.PropertyChanged += OnHandleShapeChanged;

        UpdateSelectionVisuals();
    }
}
