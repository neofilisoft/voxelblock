// VoxelBlock.Editor — Controls.cs
// Custom Avalonia controls for the editor UI.
//
// Fix log vs OpenGlViewport.cs:
//   ① Removed GetBindingObservable() → use OnPropertyChanged() override (correct Avalonia API)
//   ② Removed OpenGlControlBase — GL context mismatch requires Phase 4 shared-context setup
//   ③ Replaced OpenGlViewport with ViewportPanel (Border placeholder, no runtime crash)
//   ④ Added proper using directives
//   ⑤ TextChanged used for TwoWay binding (no Rx dependency)

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace VoxelBlock.Editor
{
    // ── LabeledField ──────────────────────────────────────────────────────────
    // Labelled property row:  [Label text]  [TextBox]
    // Supports TwoWay binding via Value property.

    public class LabeledField : UserControl
    {
        // ── Styled properties ─────────────────────────────────────────────────
        public static readonly StyledProperty<string?> LabelProperty =
            AvaloniaProperty.Register<LabeledField, string?>(nameof(Label));

        public static readonly StyledProperty<string?> ValueProperty =
            AvaloniaProperty.Register<LabeledField, string?>(
                nameof(Value), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<LabeledField, bool>(nameof(IsReadOnly));

        public string? Label      { get => GetValue(LabelProperty);      set => SetValue(LabelProperty,      value); }
        public string? Value      { get => GetValue(ValueProperty);      set => SetValue(ValueProperty,      value); }
        public bool    IsReadOnly { get => GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

        // ── Internal controls ─────────────────────────────────────────────────
        private readonly TextBlock _label;
        private readonly TextBox   _textBox;
        private bool _updating; // guard against echo loops

        public LabeledField()
        {
            _label = new TextBlock
            {
                Foreground          = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                VerticalAlignment   = VerticalAlignment.Center,
                Margin              = new Thickness(0, 0, 6, 0),
            };

            _textBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2),
            };

            // TwoWay: TextBox → Value (user edits)
            _textBox.TextChanged += (_, _) =>
            {
                if (_updating) return;
                _updating = true;
                if (Value != _textBox.Text) Value = _textBox.Text;
                _updating = false;
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("110,*"),
                Margin = new Thickness(0, 2),
            };
            Grid.SetColumn(_label,   0);
            Grid.SetColumn(_textBox, 1);
            grid.Children.Add(_label);
            grid.Children.Add(_textBox);

            Content = grid;
        }

        // ── Avalonia property change hook ─────────────────────────────────────
        // FIX ①: GetBindingObservable() does not exist. 
        // Correct pattern: override OnPropertyChanged on AvaloniaObject.
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == LabelProperty)
                _label.Text = change.GetNewValue<string?>();

            else if (change.Property == ValueProperty)
            {
                if (_updating) return;
                var v = change.GetNewValue<string?>();
                if (_textBox.Text != v)
                {
                    _updating = true;
                    _textBox.Text = v;
                    _updating = false;
                }
            }
            else if (change.Property == IsReadOnlyProperty)
                _textBox.IsReadOnly = change.GetNewValue<bool>();
        }
    }

    // ── StatRow ───────────────────────────────────────────────────────────────
    // Single read-only stat line:  [Label]  [Value (green)]

    public class StatRow : UserControl
    {
        public static readonly StyledProperty<string?> LabelProperty =
            AvaloniaProperty.Register<StatRow, string?>(nameof(Label));

        public static readonly StyledProperty<string?> ValueProperty =
            AvaloniaProperty.Register<StatRow, string?>(nameof(Value));

        public string? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        private readonly TextBlock _label;
        private readonly TextBlock _value;

        public StatRow()
        {
            _label = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                VerticalAlignment = VerticalAlignment.Center,
            };
            _value = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(144, 238, 144)), // LightGreen
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("140,*"),
                Margin = new Thickness(0, 2),
            };
            Grid.SetColumn(_label, 0);
            Grid.SetColumn(_value, 1);
            grid.Children.Add(_label);
            grid.Children.Add(_value);

            Content = grid;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == LabelProperty) _label.Text = change.GetNewValue<string?>();
            else if (change.Property == ValueProperty) _value.Text = change.GetNewValue<string?>();
        }
    }

    // ── ViewportPanel ─────────────────────────────────────────────────────────
    // FIX ②③⑤⑥: Replaces OpenGlViewport (which had GL context mismatch + incomplete VAO).
    // Phase 3: placeholder with engine stats overlay.
    // Phase 4: replace with shared-context GL blit (add vb_engine_read_pixels to C-API).

    public class ViewportPanel : Border
    {
        public static readonly StyledProperty<long> EngineHandleProperty =
            AvaloniaProperty.Register<ViewportPanel, long>(nameof(EngineHandle));

        public long EngineHandle
        {
            get => GetValue(EngineHandleProperty);
            set => SetValue(EngineHandleProperty, value);
        }

        public ViewportPanel()
        {
            Background = new SolidColorBrush(Color.FromRgb(10, 10, 10));
            ClipToBounds = true;

            Child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "3D Viewport",
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        FontSize = 18,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = "Phase 4: connect vb_engine_read_pixels\nor GL shared-context blit",
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                        FontSize = 11,
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                }
            };
        }
    }

    // Visual scene grid painter for block placement (2D top-down layer editor).
    // Left-drag paints using SceneEditorState selected block, right-drag erases.
    public class SceneGridEditor : Control
    {
        public static readonly StyledProperty<SceneEditorState?> SceneProperty =
            AvaloniaProperty.Register<SceneGridEditor, SceneEditorState?>(nameof(Scene));

        public SceneEditorState? Scene
        {
            get => GetValue(SceneProperty);
            set => SetValue(SceneProperty, value);
        }

        private SceneEditorState? _subscribed;
        private bool _painting;
        private bool _eraseDrag;
        private bool _restoreEraseMode;
        private bool _previousEraseMode;
        private int _hoverX = -1;
        private int _hoverZ = -1;

        public SceneGridEditor()
        {
            MinWidth = 320;
            MinHeight = 320;
            Focusable = true;
            PointerPressed += _onPointerPressed;
            PointerMoved += _onPointerMoved;
            PointerReleased += _onPointerReleased;
            PointerCaptureLost += (_, _) =>
            {
                _painting = false;
                if (_restoreEraseMode && Scene is not null)
                    Scene.EraseMode = _previousEraseMode;
                _restoreEraseMode = false;
            };
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property != SceneProperty) return;

            if (_subscribed is not null)
                _subscribed.SceneChanged -= _sceneChanged;

            _subscribed = change.GetNewValue<SceneEditorState?>();
            if (_subscribed is not null)
                _subscribed.SceneChanged += _sceneChanged;

            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var rect = new Rect(Bounds.Size);
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(22, 22, 22)), rect);

            var scene = Scene;
            if (scene is null || scene.Columns <= 0 || scene.Rows <= 0)
            {
                return;
            }

            double cellW = Bounds.Width / scene.Columns;
            double cellH = Bounds.Height / scene.Rows;
            if (cellW <= 0 || cellH <= 0) return;

            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 1);
            var hoverPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 208, 80)), 2);
            var emptyBrush = new SolidColorBrush(Color.FromRgb(35, 35, 35));

            for (int z = 0; z < scene.Rows; z++)
            {
                for (int x = 0; x < scene.Columns; x++)
                {
                    var cell = scene.GetCell(x, z);
                    var r = new Rect(x * cellW, z * cellH, cellW, cellH);
                    if (cell.IsEmpty)
                    {
                        context.FillRectangle(emptyBrush, r);
                    }
                    else
                    {
                        context.FillRectangle(new SolidColorBrush(Color.FromRgb(cell.R, cell.G, cell.B)), r.Deflate(1));
                    }
                    context.DrawRectangle(null, gridPen, r);
                }
            }

            if (_hoverX >= 0 && _hoverZ >= 0 && _hoverX < scene.Columns && _hoverZ < scene.Rows)
            {
                var hover = new Rect(_hoverX * cellW, _hoverZ * cellH, cellW, cellH).Deflate(1);
                context.DrawRectangle(null, hoverPen, hover);
            }
        }

        private void _onPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var scene = Scene;
            if (scene is null) return;

            var pt = e.GetPosition(this);
            if (!TryCellAt(pt, scene, out int x, out int z)) return;

            _painting = true;
            _eraseDrag = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            _previousEraseMode = scene.EraseMode;
            _restoreEraseMode = _eraseDrag;
            if (_eraseDrag)
                scene.EraseMode = true;

            scene.PaintAt(x, z);
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        private void _onPointerMoved(object? sender, PointerEventArgs e)
        {
            var scene = Scene;
            if (scene is null) return;

            var pt = e.GetPosition(this);
            if (!TryCellAt(pt, scene, out int x, out int z))
            {
                if (_hoverX != -1 || _hoverZ != -1)
                {
                    _hoverX = _hoverZ = -1;
                    InvalidateVisual();
                }
                return;
            }

            if (_hoverX != x || _hoverZ != z)
            {
                _hoverX = x;
                _hoverZ = z;
                InvalidateVisual();
            }

            if (!_painting) return;
            scene.EraseMode = _eraseDrag ? true : _previousEraseMode;
            scene.PaintAt(x, z);
        }

        private void _onPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _painting = false;
            if (_restoreEraseMode && Scene is not null)
                Scene.EraseMode = _previousEraseMode;
            _restoreEraseMode = false;
            e.Pointer.Capture(null);
        }

        private bool TryCellAt(Point pt, SceneEditorState scene, out int x, out int z)
        {
            x = z = -1;
            if (Bounds.Width <= 0 || Bounds.Height <= 0) return false;
            if (pt.X < 0 || pt.Y < 0 || pt.X >= Bounds.Width || pt.Y >= Bounds.Height) return false;

            double cellW = Bounds.Width / scene.Columns;
            double cellH = Bounds.Height / scene.Rows;
            if (cellW <= 0 || cellH <= 0) return false;

            x = (int)(pt.X / cellW);
            z = (int)(pt.Y / cellH);
            if (x < 0 || z < 0 || x >= scene.Columns || z >= scene.Rows) return false;
            return true;
        }

        private void _sceneChanged() => InvalidateVisual();
    }
}
