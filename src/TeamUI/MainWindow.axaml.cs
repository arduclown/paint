using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GraphicEditor.Common.Models;
using GraphicEditor.TeamImport;
using GraphicEditor.ViewModels;

namespace GraphicEditor;

public partial class MainWindow : Window
{
    private MainWindowViewModel VM => (DataContext as MainWindowViewModel)!;

    private bool _isDrawing;
    private Point _drawStart;

    private bool _isDragging;
    private ShapeViewModel? _dragTarget;
    private Point _dragLastPos;

    private bool _isResizing;
    private Point _resizeCenter;
    private double _resizeStartDist;
    private double _resizeLastRatio;

    private bool _isRotating;
    private double _rotateStartAngle;

    private readonly Avalonia.Controls.Shapes.Rectangle[] _handles = new Avalonia.Controls.Shapes.Rectangle[4];
    private ShapeViewModel? _handleShape;

    private Ellipse? _rotationHandle;
    private Line? _rotationLine;

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
                if (isFill) VM.ApplyFillColor(captured);
                else VM.ApplyStrokeColor(captured);
                RebuildRecentColors();
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

        _isResizing = true;
        var b = VM.SelectedShape.Bounds;
        _resizeCenter = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
        _resizeStartDist = Math.Max(1, Dist(_resizeCenter, e.GetPosition(DrawingCanvas)));
        _resizeLastRatio = 1.0;

        e.Pointer.Capture(DrawingCanvas);
        e.Handled = true;
    }

    private void RotationHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        if (VM.SelectedShape is null) return;

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

    private static Point ApplyShiftConstraint(ToolType tool, Point start, Point current)
    {
        double dx = current.X - start.X;
        double dy = current.Y - start.Y;

        switch (tool)
        {
            case ToolType.Rectangle:
            case ToolType.Triangle:
            {
                double size = Math.Max(Math.Abs(dx), Math.Abs(dy));
                return new Point(start.X + Math.Sign(dx) * size,
                                 start.Y + Math.Sign(dy) * size);
            }
            case ToolType.Line:
            {
                double len = Math.Sqrt(dx * dx + dy * dy);
                double angle = Math.Atan2(dy, dx);
                double snapped = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);
                return new Point(start.X + len * Math.Cos(snapped),
                                 start.Y + len * Math.Sin(snapped));
            }
            default:
                return current;
        }
    }

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        DrawingCanvas.Focus();
        var pos = e.GetPosition(DrawingCanvas);

        if (VM.CurrentTool == ToolType.Select)
        {
            VM.SelectedShape = null;
            return;
        }

        if (VM.ActiveLayer?.IsLocked == true) return;
        if (VM.SnapEnabled) pos = SnapToGrid(pos);

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

        if (_isDrawing)
        {
            if (VM.SnapEnabled) pos = SnapToGrid(pos);
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                pos = ApplyShiftConstraint(VM.CurrentTool, _drawStart, pos);
            UpdatePreview(pos);
        }
        else if (_isRotating && VM.SelectedShape is not null)
        {
            var b = VM.SelectedShape.Bounds;
            var center = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
            double currentAngle = Math.Atan2(pos.Y - center.Y, pos.X - center.X) * 180.0 / Math.PI;
            double delta = currentAngle - _rotateStartAngle;

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                delta = Math.Round(delta / 15.0) * 15.0;

            if (Math.Abs(delta) > 0.1)
            {
                VM.SelectedShape.Rotate(delta);
                _rotateStartAngle = currentAngle;
            }
        }
        else if (_isResizing && VM.SelectedShape is not null)
        {
            double dist = Dist(_resizeCenter, pos);
            if (dist >= 5 && _resizeStartDist >= 1)
            {
                double ratio = dist / _resizeStartDist;
                double increment = ratio / _resizeLastRatio;
                if (increment > 0.05 && increment < 20)
                {
                    VM.SelectedShape.Scale(increment);
                    _resizeLastRatio = ratio;
                }
            }
        }
        else if (_isDragging && _dragTarget is not null)
        {
            var delta = new Point(pos.X - _dragLastPos.X, pos.Y - _dragLastPos.Y);
            _dragTarget.Move(delta);
            _dragLastPos = pos;
        }
    }

    private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);

        if (_isDrawing)
        {
            _isDrawing = false;
            PreviewPath.IsVisible = false;
            e.Pointer.Capture(null);

            if (VM.SnapEnabled) pos = SnapToGrid(pos);
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                pos = ApplyShiftConstraint(VM.CurrentTool, _drawStart, pos);

            var shape = VM.CreateShape(VM.CurrentTool, _drawStart, pos);
            VM.AddShape(shape);
            VM.SelectedShape = shape;
        }

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

    private void Shape_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;

        if (sender is Avalonia.Controls.Shapes.Path path
            && path.DataContext is ShapeViewModel vm)
        {
            // Клик по фигуре всегда выделяет и начинает перетаскивание
            VM.SelectedShape = vm;
            VM.CurrentTool = ToolType.Select;

            _isDragging = true;
            _dragTarget = vm;
            _dragLastPos = e.GetPosition(DrawingCanvas);
            e.Pointer.Capture(DrawingCanvas);
            e.Handled = true;
        }
    }

    private void UpdatePreview(Point current)
    {
        string? pathData = VM.CurrentTool switch
        {
            ToolType.Circle    => BuildCirclePreview(_drawStart, current),
            ToolType.Rectangle => BuildRectPreview(_drawStart, current),
            ToolType.Triangle  => BuildTrianglePreview(_drawStart, current),
            ToolType.Line      => BuildLinePreview(_drawStart, current),
            _                  => null,
        };

        if (pathData is not null)
        {
            try { PreviewPath.Data = Geometry.Parse(pathData); }
            catch { PreviewPath.Data = null; }
        }
    }

    private Point SnapToGrid(Point p)
    {
        double step = VM.GridStep;
        return new Point(
            Math.Round(p.X / step, MidpointRounding.AwayFromZero) * step,
            Math.Round(p.Y / step, MidpointRounding.AwayFromZero) * step);
    }

    private static string BuildCirclePreview(Point center, Point edge)
    {
        double dx = edge.X - center.X;
        double dy = edge.Y - center.Y;
        double r = Math.Max(3, Math.Sqrt(dx * dx + dy * dy));
        return FormattableString.Invariant(
            $"M {center.X - r:F2},{center.Y:F2} A {r:F2},{r:F2},0,1,0,{center.X + r:F2},{center.Y:F2} A {r:F2},{r:F2},0,1,0,{center.X - r:F2},{center.Y:F2} Z");
    }

    private static string BuildRectPreview(Point p1, Point p2)
    {
        double x1 = Math.Min(p1.X, p2.X), y1 = Math.Min(p1.Y, p2.Y);
        double x2 = Math.Max(p1.X, p2.X), y2 = Math.Max(p1.Y, p2.Y);
        return FormattableString.Invariant(
            $"M {x1:F2},{y1:F2} H {x2:F2} V {y2:F2} H {x1:F2} Z");
    }

    private static string BuildTrianglePreview(Point p1, Point p2)
    {
        double x1 = Math.Min(p1.X, p2.X), x2 = Math.Max(p1.X, p2.X);
        double y1 = Math.Min(p1.Y, p2.Y), y2 = Math.Max(p1.Y, p2.Y);
        double cx = (x1 + x2) / 2.0;
        return FormattableString.Invariant(
            $"M {cx:F2},{y1:F2} L {x1:F2},{y2:F2} L {x2:F2},{y2:F2} Z");
    }

    private static string BuildLinePreview(Point p1, Point p2) =>
        FormattableString.Invariant($"M {p1.X:F2},{p1.Y:F2} L {p2.X:F2},{p2.Y:F2}");

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

    private void RebuildRecentColors()
    {
        RecentColorsPanel.Children.Clear();
        foreach (var color in VM.RecentColors)
        {
            var btn = new Button
            {
                Width = 20, Height = 20,
                Margin = new Thickness(1),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
            };
            var c = color;
            btn.Click += (_, _) =>
            {
                VM.ApplyFillColor(c);
                RebuildRecentColors();
            };
            RecentColorsPanel.Children.Add(btn);
        }
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
        if (DataContext is MainWindowViewModel vm)
            vm.PropertyChanged += ViewModel_PropertyChanged;
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
            var dlg = new Window
            {
                Title = "Ошибка импорта",
                Width = 420, Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = ex.Message,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.OrangeRed,
                }
            };
            await dlg.ShowDialog(this);
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
            var dlg = new Window
            {
                Title = "Ошибка экспорта",
                Width = 420, Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = ex.Message,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.OrangeRed,
                }
            };
            await dlg.ShowDialog(this);
        }
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
            UpdateHandles(shape);
            UpdateSelectionRect(shape);
        }
    }

    private void OnHandleShapeChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShapeViewModel.Bounds))
        {
            UpdateHandles(VM.SelectedShape);
            UpdateSelectionRect(VM.SelectedShape);
        }
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

        switch (e.Key)
        {
            case Key.S: VM.CurrentTool = ToolType.Select;    e.Handled = true; break;
            case Key.C: VM.CurrentTool = ToolType.Circle;    e.Handled = true; break;
            case Key.R: VM.CurrentTool = ToolType.Rectangle; e.Handled = true; break;
            case Key.T: VM.CurrentTool = ToolType.Triangle;  e.Handled = true; break;
            case Key.L: VM.CurrentTool = ToolType.Line;      e.Handled = true; break;
        }
    }

    private void Canvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        VM.ZoomFactor += e.Delta.Y > 0 ? 0.1 : -0.1;
        e.Handled = true;
    }

    private void UpdateSelectionRect(ShapeViewModel? shape)
    {
        if (shape is null)
        {
            SelectionRect.IsVisible = false;
            return;
        }

        var b = shape.Bounds;
        const double pad = 4;
        Canvas.SetLeft(SelectionRect, b.Left - pad);
        Canvas.SetTop(SelectionRect, b.Top - pad);
        SelectionRect.Width = b.Width + pad * 2;
        SelectionRect.Height = b.Height + pad * 2;
        SelectionRect.IsVisible = true;
    }
}
