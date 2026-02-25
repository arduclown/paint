using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    // ─── Состояние рисования ───
    private bool _isDrawing;
    private Point _drawStart;

    // ─── Состояние перетаскивания ───
    private bool _isDragging;
    private ShapeViewModel? _dragTarget;
    private Point _dragLastPos;

    // ─── Состояние масштабирования маркерами ───
    private bool _isResizing;
    private Point _resizeCenter;
    private double _resizeStartDist;
    private double _resizeLastRatio;

    // ─── Маркеры масштабирования ───
    private readonly Ellipse[] _handles = new Ellipse[4];
    private ShapeViewModel? _handleShape;

    // ─── Палитра цветов ───
    private static readonly (Color color, string name)[] Palette =
    {
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
    };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, EventArgs e)
    {
        BuildColorPalette(FillColorPanel, isFill: true);
        BuildColorPalette(StrokeColorPanel, isFill: false);
        DrawGrid();
        InitHandles();
    }

    // ─── Строим палитру цветов ───
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

            // Для "Без заливки" — крестик
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
            };

            panel.Children.Add(btn);
        }
    }

    // ─── Рисуем сетку на GridCanvas ───
    private void DrawGrid()
    {
        const double step = 40;
        const double w = 1600, h = 1200;

        var geomGroup = new GeometryGroup();

        for (double x = 0; x <= w; x += step)
            geomGroup.Children.Add(new LineGeometry(new Point(x, 0), new Point(x, h)));

        for (double y = 0; y <= h; y += step)
            geomGroup.Children.Add(new LineGeometry(new Point(0, y), new Point(w, y)));

        var gridPath = new Avalonia.Controls.Shapes.Path
        {
            Data = geomGroup,
            Stroke = new SolidColorBrush(Color.FromArgb(35, 150, 150, 150)),
            StrokeThickness = 0.5,
            IsHitTestVisible = false,
        };

        GridCanvas.Children.Add(gridPath);
    }

    // ══════════════════════════════════════════════
    //  МАРКЕРЫ МАСШТАБИРОВАНИЯ
    // ══════════════════════════════════════════════

    private void InitHandles()
    {
        for (int i = 0; i < 4; i++)
        {
            var h = new Ellipse
            {
                Width = 10, Height = 10,
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
    }

    private void UpdateHandles(ShapeViewModel? shape)
    {
        if (shape == null)
        {
            foreach (var h in _handles) h.IsVisible = false;
            return;
        }

        var b = shape.Bounds;
        // Порядок: TL, TR, BR, BL
        double[] xs = { b.Left, b.Right, b.Right, b.Left };
        double[] ys = { b.Top, b.Top, b.Bottom, b.Bottom };

        for (int i = 0; i < 4; i++)
        {
            Canvas.SetLeft(_handles[i], xs[i] - 5);
            Canvas.SetTop(_handles[i], ys[i] - 5);
            _handles[i].IsVisible = true;
        }
    }

    private void Handle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        if (VM.SelectedShape == null) return;

        _isResizing = true;
        var b = VM.SelectedShape.Bounds;
        _resizeCenter = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
        _resizeStartDist = Math.Max(1, Dist(_resizeCenter, e.GetPosition(DrawingCanvas)));
        _resizeLastRatio = 1.0;

        e.Pointer.Capture(DrawingCanvas);
        e.Handled = true;
    }

    private static double Dist(Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // ══════════════════════════════════════════════
    //  SHIFT-ОГРАНИЧЕНИЕ ПРИ РИСОВАНИИ
    // ══════════════════════════════════════════════

    private static Point ApplyShiftConstraint(ToolType tool, Point start, Point current)
    {
        double dx = current.X - start.X;
        double dy = current.Y - start.Y;

        switch (tool)
        {
            case ToolType.Rectangle:
            case ToolType.Triangle:
            {
                // Квадратный ограничивающий прямоугольник
                double size = Math.Max(Math.Abs(dx), Math.Abs(dy));
                return new Point(start.X + Math.Sign(dx) * size,
                                 start.Y + Math.Sign(dy) * size);
            }
            case ToolType.Line:
            {
                // Привязка к ближайшему кратному 45°
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

    // ══════════════════════════════════════════════
    //  СОБЫТИЯ ХОЛСТА
    // ══════════════════════════════════════════════

    private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;

        var pos = e.GetPosition(DrawingCanvas);

        if (VM.CurrentTool == ToolType.Select)
        {
            // Клик на пустом месте — снимаем выделение
            VM.SelectedShape = null;
            return;
        }

        if (VM.ActiveLayer?.IsLocked == true) return;

        if (VM.SnapEnabled) pos = SnapToGrid(pos);

        // Режим рисования
        _isDrawing = true;
        _drawStart = pos;
        PreviewPath.IsVisible = true;
        UpdatePreview(pos);
        e.Pointer.Capture(DrawingCanvas);
    }

    private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(DrawingCanvas);

        if (_isDrawing)
        {
            if (VM.SnapEnabled) pos = SnapToGrid(pos);
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                pos = ApplyShiftConstraint(VM.CurrentTool, _drawStart, pos);
            UpdatePreview(pos);
        }
        else if (_isResizing && VM.SelectedShape != null)
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
        else if (_isDragging && _dragTarget != null)
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

            // Создаём фигуру и добавляем через команду (с поддержкой Undo)
            var shape = VM.CreateShape(VM.CurrentTool, _drawStart, pos);
            VM.AddShape(shape);
            VM.SelectedShape = shape;
        }

        if (_isResizing)
        {
            _isResizing = false;
            e.Pointer.Capture(null);
        }

        if (_isDragging && _dragTarget != null)
        {
            _isDragging = false;
            _dragTarget = null;
            e.Pointer.Capture(null);
        }
    }

    // ─── Клик по фигуре ───
    private void Shape_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;

        if (sender is Avalonia.Controls.Shapes.Path path
            && path.DataContext is ShapeViewModel vm)
        {
            if (VM.CurrentTool == ToolType.Select)
            {
                VM.SelectedShape = vm;

                // Начинаем перетаскивание
                _isDragging = true;
                _dragTarget = vm;
                _dragLastPos = e.GetPosition(DrawingCanvas);
                e.Pointer.Capture(DrawingCanvas);

                // Предотвращаем всплытие к Canvas (иначе снимет выделение)
                e.Handled = true;
            }
        }
    }

    // ─── Превью фигуры при рисовании ───
    private void UpdatePreview(Point current)
    {
        string? pathData = VM.CurrentTool switch
        {
            ToolType.Circle => BuildCirclePreview(_drawStart, current),
            ToolType.Rectangle => BuildRectPreview(_drawStart, current),
            ToolType.Triangle => BuildTrianglePreview(_drawStart, current),
            ToolType.Line => BuildLinePreview(_drawStart, current),
            _ => null,
        };

        if (pathData != null)
        {
            try
            {
                PreviewPath.Data = Geometry.Parse(pathData);
            }
            catch
            {
                PreviewPath.Data = null;
            }
        }
    }

    private static Point SnapToGrid(Point p)
    {
        const double step = 40.0;
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

    private static string BuildLinePreview(Point p1, Point p2)
        => FormattableString.Invariant($"M {p1.X:F2},{p1.Y:F2} L {p2.X:F2},{p2.Y:F2}");

    // ══════════════════════════════════════════════
    //  СОБЫТИЯ ПАНЕЛИ СВОЙСТВ
    // ══════════════════════════════════════════════

    private void ShapeNameBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && VM.SelectedShape != null
            && tb.Text != VM.SelectedShape.Name)
        {
            VM.SelectedShape.Name = tb.Text ?? "";
        }
    }

    private void OpacitySlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (VM.SelectedShape != null)
            VM.SelectedShape.Opacity = e.NewValue;
    }

    private void RotateLeft_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => VM.RotateSelected(-15);

    private void RotateRight_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => VM.RotateSelected(15);

    private void MirrorX_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => VM.MirrorXSelected();

    private void MirrorY_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => VM.MirrorYSelected();

    // ─── Синхронизация UI при смене выделенной фигуры ───
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
            vm.PropertyChanged += ViewModel_PropertyChanged;
    }

    // ══════════════════════════════════════════════
    //  ИМПОРТ (JSON)
    // ══════════════════════════════════════════════

    private async void ImportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Импортировать сцену",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("JSON — данные редактора") { Patterns = new[] { "*.json" } },
            }
        });

        if (files.Count == 0) return;

        try
        {
            var path = files[0].Path.LocalPath;
            VM.ImportJson(path);
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

    // ══════════════════════════════════════════════
    //  ЭКСПОРТ (SVG / PDF / JSON)
    // ══════════════════════════════════════════════

    private async void ExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспортировать сцену",
            SuggestedFileName = "scene",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("SVG — векторная графика") { Patterns = new[] { "*.svg" } },
                new FilePickerFileType("PDF — документ")          { Patterns = new[] { "*.pdf" } },
                new FilePickerFileType("JSON — данные редактора") { Patterns = new[] { "*.json" } },
            }
        });

        if (file == null) return;

        try
        {
            var path = file.Path.LocalPath;
            switch (System.IO.Path.GetExtension(path).ToLowerInvariant())
            {
                case ".svg": SvgExporter.Export(VM.Shapes, path); break;
                case ".pdf": PdfExporter.Export(VM.Shapes, path); break;
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

    // ─── Клик по фигуре внутри раскрытого слоя ───
    private void LayerShape_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;
        if (sender is Avalonia.Controls.Border border && border.DataContext is ShapeViewModel shape)
        {
            VM.SelectedShape = shape;
            e.Handled = true;
        }
    }

    // ─── Клик по имени слоя → делаем слой активным ───
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
            if (shape != null)
                OpacitySlider.Value = shape.Opacity;

            _suppressLayerComboChange = true;
            LayerComboBox.SelectedItem = shape != null
                ? VM.Layers.FirstOrDefault(l => l.Name == shape.LayerName)
                : null;
            _suppressLayerComboChange = false;

            // Обновляем маркеры масштабирования
            if (_handleShape != null)
                _handleShape.PropertyChanged -= OnHandleShapeChanged;
            _handleShape = shape;
            if (_handleShape != null)
                _handleShape.PropertyChanged += OnHandleShapeChanged;
            UpdateHandles(shape);
        }
    }

    private void OnHandleShapeChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShapeViewModel.Bounds))
            UpdateHandles(VM.SelectedShape);
    }
}
