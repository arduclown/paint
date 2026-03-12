using System;
using System.Linq;
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
    private MainWindowViewModel VM => (DataContext as MainWindowViewModel)!;

    private bool _isDrawing;
    private Point _drawStart;

    private bool _isDragging;
    private ShapeViewModel? _dragTarget;
    private Point _dragLastPos;
    private Point _dragAnchorOffset; // offset from pointer to shape's top-left at drag start

    private bool _isResizing;
    private int _resizeHandleIndex = -1;
    private Point _resizeAnchor;
    private Rect _resizeStartBounds;

    private bool _isRotating;
    private double _rotateStartAngle;

    private readonly Avalonia.Controls.Shapes.Rectangle[] _handles = new Avalonia.Controls.Shapes.Rectangle[4];
    private ShapeViewModel? _handleShape;

    private Ellipse? _rotationHandle;
    private Line? _rotationLine;

    private bool _isListDragging;
    private int _listDragFromIndex = -1;
    private Point _listDragStartPos;

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

        ShapesListBox.PointerPressed  += ShapesList_PointerPressed;
        ShapesListBox.PointerMoved    += ShapesList_PointerMoved;
        ShapesListBox.PointerReleased += ShapesList_PointerReleased;
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
        StandardCursorType[] handleCursors =
        [
            StandardCursorType.TopLeftCorner,
            StandardCursorType.TopRightCorner,
            StandardCursorType.BottomRightCorner,
            StandardCursorType.BottomLeftCorner,
        ];

        for (int i = 0; i < 4; i++)
        {
            var h = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = 8, Height = 8,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromRgb(86, 156, 214)),
                StrokeThickness = 1.5,
                IsVisible = false,
                Cursor = new Cursor(handleCursors[i]),
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
        _resizeHandleIndex = Array.IndexOf(_handles, sender as Avalonia.Controls.Shapes.Rectangle);

        var b = VM.SelectedShape.Bounds;
        _resizeStartBounds = b;

        // Anchor = opposite corner (stays fixed during drag)
        // Handle layout: 0=TL, 1=TR, 2=BR, 3=BL
        _resizeAnchor = _resizeHandleIndex switch
        {
            0 => new Point(b.Right, b.Bottom),
            1 => new Point(b.Left,  b.Bottom),
            2 => new Point(b.Left,  b.Top),
            3 => new Point(b.Right, b.Top),
            _ => new Point(b.X + b.Width / 2, b.Y + b.Height / 2),
        };

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
            var shape = VM.SelectedShape;
            var b = shape.Bounds;
            if (b.Width < 1 || b.Height < 1) return;

            double newWidth  = Math.Max(5, Math.Abs(pos.X - _resizeAnchor.X));
            double newHeight = Math.Max(5, Math.Abs(pos.Y - _resizeAnchor.Y));

            double scaleX = newWidth  / b.Width;
            double scaleY = newHeight / b.Height;

            // Shift → uniform scale (proportional), larger axis wins
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                double uniform = Math.Max(scaleX, scaleY);
                scaleX = uniform;
                scaleY = uniform;
                newWidth  = b.Width  * uniform;
                newHeight = b.Height * uniform;
            }

            if (scaleX < 0.01 || scaleX > 50 || scaleY < 0.01 || scaleY > 50) return;

            shape.ScaleXY(scaleX, scaleY);

            // After ScaleXY the shape scales around its bounds center.
            // Move the shape so the anchor corner stays fixed.
            double cx = b.X + b.Width  / 2;
            double cy = b.Y + b.Height / 2;

            // Where the anchor corner ends up after ScaleXY (before the move):
            // 0=TL dragged → anchor BR = (cx + W/2*sx, cy + H/2*sy)
            // 1=TR dragged → anchor BL = (cx - W/2*sx, cy + H/2*sy)
            // 2=BR dragged → anchor TL = (cx - W/2*sx, cy - H/2*sy)
            // 3=BL dragged → anchor TR = (cx + W/2*sx, cy - H/2*sy)
            Point anchorAfter = _resizeHandleIndex switch
            {
                0 => new Point(cx + b.Width / 2 * scaleX, cy + b.Height / 2 * scaleY),
                1 => new Point(cx - b.Width / 2 * scaleX, cy + b.Height / 2 * scaleY),
                2 => new Point(cx - b.Width / 2 * scaleX, cy - b.Height / 2 * scaleY),
                3 => new Point(cx + b.Width / 2 * scaleX, cy - b.Height / 2 * scaleY),
                _ => new Point(cx, cy),
            };

            shape.Move(new Point(_resizeAnchor.X - anchorAfter.X,
                                 _resizeAnchor.Y - anchorAfter.Y));
        }
        else if (_isDragging && _dragTarget is not null)
        {
            if (VM.SnapEnabled)
            {
                // Snap the shape's top-left corner to the grid
                var targetTL = SnapToGrid(new Point(pos.X - _dragAnchorOffset.X,
                                                    pos.Y - _dragAnchorOffset.Y));
                var b = _dragTarget.Bounds;
                var delta = new Point(targetTL.X - b.X, targetTL.Y - b.Y);
                if (delta.X != 0 || delta.Y != 0)
                    _dragTarget.Move(delta);
            }
            else
            {
                var delta = new Point(pos.X - _dragLastPos.X, pos.Y - _dragLastPos.Y);
                _dragTarget.Move(delta);
                _dragLastPos = pos;
            }
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

    // ─── Drag-and-drop порядка фигур в левой панели ──────────────────────────

    private void ShapesList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ShapesListBox).Properties.IsLeftButtonPressed) return;
        var pos = e.GetPosition(ShapesListBox);
        if (!IsPointerOnDragHandle(pos)) return;

        var idx = GetListItemIndexAtPoint(pos);
        if (idx < 0) return;

        // Выделяем фигуру вручную и перехватываем событие, чтобы ListBox
        // не переключал выделение сам по себе при начале перетаскивания
        VM.SelectedShape = VM.Shapes[idx];

        _listDragFromIndex = idx;
        _listDragStartPos  = pos;
        _isListDragging    = true;

        ShapesListBox.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
        e.Pointer.Capture(ShapesListBox);
        e.Handled = true;
    }

    private void ShapesList_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isListDragging || _listDragFromIndex < 0) return;

        var pos = e.GetPosition(ShapesListBox);
        var overIndex = GetListItemIndexAtPoint(pos);
        UpdateDragIndicator(overIndex);
    }

    private void ShapesList_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        DragDropCanvas.IsVisible = false;
        DragDropCanvas.Children.Clear();
        ShapesListBox.Cursor = null;

        if (!_isListDragging || _listDragFromIndex < 0)
        {
            _listDragFromIndex = -1;
            _isListDragging    = false;
            return;
        }

        var toIndex = GetListItemIndexAtPoint(e.GetPosition(ShapesListBox));
        if (toIndex >= 0 && toIndex != _listDragFromIndex)
            VM.MoveShapeOrder(_listDragFromIndex, toIndex);

        _listDragFromIndex = -1;
        _isListDragging    = false;
        e.Pointer.Capture(null);
    }

    /// Проверяет, что клик пришёл с элемента-ручки (Tag == "draghandle")
    private bool IsPointerOnDragHandle(Point posInListBox)
    {
        var hit = ShapesListBox.InputHitTest(posInListBox) as Visual;
        while (hit is not null)
        {
            if (hit is Control ctrl && ctrl.Tag is "draghandle")
                return true;
            if (hit is ListBoxItem)
                break;
            hit = hit.GetVisualParent();
        }
        return false;
    }

    private int GetListItemIndexAtPoint(Point posInListBox)
    {
        for (int i = 0; i < VM.Shapes.Count; i++)
        {
            if (ShapesListBox.ContainerFromIndex(i) is not Visual container) continue;
            var transform = container.TransformToVisual(ShapesListBox);
            if (transform is null) continue;
            var top    = transform.Value.Transform(new Point(0, 0)).Y;
            var bottom = top + container.Bounds.Height;
            if (posInListBox.Y >= top && posInListBox.Y < bottom)
                return i;
        }
        return -1;
    }

    private void UpdateDragIndicator(int overIndex)
    {
        DragDropCanvas.Children.Clear();
        if (overIndex < 0)
        {
            DragDropCanvas.IsVisible = false;
            return;
        }

        if (ShapesListBox.ContainerFromIndex(overIndex) is not Visual container)
        {
            DragDropCanvas.IsVisible = false;
            return;
        }

        var transform = container.TransformToVisual(DragDropCanvas);
        if (transform is null) return;

        var top    = transform.Value.Transform(new Point(0, 0)).Y;
        double lineY = overIndex > _listDragFromIndex ? top + container.Bounds.Height : top;

        DragDropCanvas.Children.Add(new Avalonia.Controls.Shapes.Line
        {
            StartPoint       = new Point(4, lineY),
            EndPoint         = new Point(DragDropCanvas.Bounds.Width - 4, lineY),
            Stroke           = new SolidColorBrush(Color.FromRgb(86, 156, 214)),
            StrokeThickness  = 2,
            IsHitTestVisible = false,
        });
        DragDropCanvas.IsVisible = true;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Shape_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(DrawingCanvas).Properties.IsLeftButtonPressed) return;

        if (sender is Avalonia.Controls.Shapes.Path path
            && path.DataContext is ShapeViewModel vm)
        {
            // Ctrl+клик — выбрать вторую фигуру для булевой операции
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                VM.SecondSelectedShape = vm != VM.SelectedShape ? vm : null;
                e.Handled = true;
                return;
            }

            // Обычный клик — выделить и начать перетаскивание
            VM.SelectedShape = vm;
            VM.CurrentTool = ToolType.Select;

            _isDragging = true;
            _dragTarget = vm;
            _dragLastPos = e.GetPosition(DrawingCanvas);
            var bounds = vm.Bounds;
            _dragAnchorOffset = new Point(_dragLastPos.X - bounds.X, _dragLastPos.Y - bounds.Y);
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