using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;

namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>
/// Paint-light sketch surface: pen, line, spline, box, circle, speech bubble, text, text box,
/// eraser, select with rotate, fuse groups, grid snap, meetup, Gridify, and image paste.
/// </summary>
public sealed class SketchControl : Control
{
    const double MinSampleDistance = 2.0;
    const double HitToleranceScreen = 8.0;
    const double GripSizeScreen = 7.0;
    const double RotateGripOffsetScreen = 28.0;
    const double MarqueeStartSlop = 4.0;
    const double MeetupScreenRadius = 14.0;
    const double DefaultFontSize = 16;

    static readonly IBrush BackgroundBrush = new ImmutableSolidColorBrush(Color.Parse("#f7f7f5"));
    static readonly IPen GridPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#d8d8d4")), 1);
    static readonly IPen GridMajorPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#c4c4be")), 1);
    static readonly IPen SelectionPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#4c8bf5")), 1.25);
    static readonly IPen MarqueePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#4c8bf5")), 1, dashStyle: new ImmutableDashStyle([4, 3], 0));
    static readonly IBrush GripBrush = new ImmutableSolidColorBrush(Color.Parse("#ffffff"));
    static readonly IPen GripPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#4c8bf5")), 1);
    static readonly IPen RubberPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#888888")), 1.5, dashStyle: new ImmutableDashStyle([6, 4], 0));
    static readonly IPen MeetupPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.Parse("#e85d04")), 1.5);

    readonly Dictionary<string, Bitmap> _imageCache = new(StringComparer.Ordinal);

    SketchDocument _document = new();
    double _offsetX;
    double _offsetY;
    double _scale = 1;
    bool _spaceDown;
    bool _panning;
    Point? _lastScreen;
    List<SketchPoint>? _draft;
    List<SketchPoint>? _splineControls;
    SketchPoint? _hoverWorld;
    SketchPoint? _meetupHint;
    DragMode _dragMode;
    GripKind _activeGrip;
    SketchPoint _dragStartWorld;
    Dictionary<string, List<SketchPoint>>? _dragOriginalPoints;
    Dictionary<string, double>? _dragOriginalRotations;
    SketchRect? _dragUnionBounds;
    Point _marqueeStartScreen;
    Rect? _marqueeScreen;
    bool _marqueeAdditive;
    DateTime _lastVertexClickUtc;
    SketchPoint? _lastVertexClickWorld;
    HashSet<string>? _eraserBatch;
    bool _pendingClose;
    TextBox? _textEditor;
    Popup? _textPopup;
    string? _editingElementId;
    bool _endingTextEdit;
    string? _lastSelectionKey;

    /// <summary>Document shown and edited by this control.</summary>
    public static readonly StyledProperty<SketchDocument?> DocumentProperty =
        AvaloniaProperty.Register<SketchControl, SketchDocument?>(nameof(Document));

    /// <summary>Active tool.</summary>
    public static readonly StyledProperty<SketchTool> ToolProperty =
        AvaloniaProperty.Register<SketchControl, SketchTool>(nameof(Tool), SketchTool.Pen);

    /// <summary>Grid cell size in world units.</summary>
    public static readonly StyledProperty<double> GridSizeProperty =
        AvaloniaProperty.Register<SketchControl, double>(nameof(GridSize), 20);

    /// <summary>Whether the grid is drawn.</summary>
    public static readonly StyledProperty<bool> GridVisibleProperty =
        AvaloniaProperty.Register<SketchControl, bool>(nameof(GridVisible), true);

    /// <summary>Whether pointer edits snap to grid intersections.</summary>
    public static readonly StyledProperty<bool> SnapEnabledProperty =
        AvaloniaProperty.Register<SketchControl, bool>(nameof(SnapEnabled));

    /// <summary>Whether pointer edits snap to existing shape vertices (meetup).</summary>
    public static readonly StyledProperty<bool> MeetupEnabledProperty =
        AvaloniaProperty.Register<SketchControl, bool>(nameof(MeetupEnabled), true);

    /// <summary>Stroke color for new shapes (#RRGGBB).</summary>
    public static readonly StyledProperty<string> StrokeColorProperty =
        AvaloniaProperty.Register<SketchControl, string>(nameof(StrokeColor), "#1e1e1e");

    /// <summary>Stroke width in world units for new shapes.</summary>
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<SketchControl, double>(nameof(StrokeWidth), 2);

    /// <summary>When true, closed shapes (box/ellipse/closed line) get a fill.</summary>
    public static readonly StyledProperty<bool> FillEnabledProperty =
        AvaloniaProperty.Register<SketchControl, bool>(nameof(FillEnabled));

    /// <summary>Fill color for new closed shapes (#RRGGBB). Empty uses <see cref="StrokeColor"/>.</summary>
    public static readonly StyledProperty<string> FillColorProperty =
        AvaloniaProperty.Register<SketchControl, string>(nameof(FillColor), "");

    /// <summary>Dash / stipple style for new strokes.</summary>
    public static readonly StyledProperty<SketchStrokeStyle> StrokeStyleProperty =
        AvaloniaProperty.Register<SketchControl, SketchStrokeStyle>(nameof(StrokeStyle));

    static SketchControl()
    {
        AffectsRender<SketchControl>(
            DocumentProperty, ToolProperty, GridSizeProperty, GridVisibleProperty, SnapEnabledProperty,
            MeetupEnabledProperty, StrokeColorProperty, StrokeWidthProperty,
            FillEnabledProperty, FillColorProperty, StrokeStyleProperty);
        DocumentProperty.Changed.AddClassHandler<SketchControl>((c, e) => c.OnDocumentChanged(e));
        ToolProperty.Changed.AddClassHandler<SketchControl>((c, _) =>
        {
            c.EndTextEdit(commit: true);
            c.CancelInProgressDrawing();
        });
        GridSizeProperty.Changed.AddClassHandler<SketchControl>((c, _) => c.SyncGridFromProperties());
        GridVisibleProperty.Changed.AddClassHandler<SketchControl>((c, _) => c.SyncGridFromProperties());
        SnapEnabledProperty.Changed.AddClassHandler<SketchControl>((c, _) => c.SyncGridFromProperties());
    }

    /// <summary>Creates the control with an empty document.</summary>
    public SketchControl()
    {
        Focusable = true;
        ClipToBounds = true;
        SetCurrentValue(DocumentProperty, _document);
        AttachDocument(_document);
    }

    /// <summary>Document shown and edited by this control.</summary>
    public SketchDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    /// <summary>Active tool.</summary>
    public SketchTool Tool
    {
        get => GetValue(ToolProperty);
        set => SetValue(ToolProperty, value);
    }

    /// <summary>Grid cell size in world units.</summary>
    public double GridSize
    {
        get => GetValue(GridSizeProperty);
        set => SetValue(GridSizeProperty, value);
    }

    /// <summary>Whether the grid is drawn.</summary>
    public bool GridVisible
    {
        get => GetValue(GridVisibleProperty);
        set => SetValue(GridVisibleProperty, value);
    }

    /// <summary>Whether pointer edits snap to grid intersections.</summary>
    public bool SnapEnabled
    {
        get => GetValue(SnapEnabledProperty);
        set => SetValue(SnapEnabledProperty, value);
    }

    /// <summary>Whether pointer edits snap to existing shape vertices (meetup).</summary>
    public bool MeetupEnabled
    {
        get => GetValue(MeetupEnabledProperty);
        set => SetValue(MeetupEnabledProperty, value);
    }

    /// <summary>Stroke color for new shapes (#RRGGBB).</summary>
    public string StrokeColor
    {
        get => GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    /// <summary>Stroke width in world units for new shapes.</summary>
    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    /// <summary>When true, closed shapes get a fill using <see cref="FillColor"/> (or stroke color).</summary>
    public bool FillEnabled
    {
        get => GetValue(FillEnabledProperty);
        set => SetValue(FillEnabledProperty, value);
    }

    /// <summary>Fill color for new closed shapes. Empty falls back to <see cref="StrokeColor"/>.</summary>
    public string FillColor
    {
        get => GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    /// <summary>Dash / stipple style for new strokes.</summary>
    public SketchStrokeStyle StrokeStyle
    {
        get => GetValue(StrokeStyleProperty);
        set => SetValue(StrokeStyleProperty, value);
    }

    /// <summary>Whether Line/Spline has an unfinished vertex path.</summary>
    public bool HasInProgressDrawing =>
        Tool is SketchTool.Line or SketchTool.Spline
        && (_draft is { Count: > 0 } || _splineControls is { Count: > 0 });

    /// <summary>Raised after the document mutates.</summary>
    public event Action? DocumentChanged;

    /// <summary>Raised when selection changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>
    /// Finishes the in-progress Line/Spline. When <paramref name="closeShape"/> is true,
    /// closes the path to the first vertex (and fills when <see cref="FillEnabled"/>).
    /// </summary>
    public void CompleteDrawing(bool closeShape = false)
    {
        if (!HasInProgressDrawing)
            return;
        _pendingClose = closeShape;
        CommitVertexTool();
        InvalidateVisual();
    }

    /// <summary>Gridifies selection (or all strokes) using the current grid size.</summary>
    public void GridifySelection()
    {
        EnsureDocument().GridifySelection();
        InvalidateVisual();
    }

    /// <summary>Fuses the current multi-selection into one group.</summary>
    public bool FuseSelection()
    {
        var ok = EnsureDocument().FuseSelection();
        if (ok)
            InvalidateVisual();
        return ok;
    }

    /// <summary>Ungroups the current selection.</summary>
    public bool UngroupSelection()
    {
        var ok = EnsureDocument().UngroupSelection();
        if (ok)
            InvalidateVisual();
        return ok;
    }

    /// <summary>
    /// Pastes a PNG (or other Avalonia-decodable raster) as an image element centered at
    /// <paramref name="centerWorld"/>, scaled so the longest edge is at most <paramref name="maxEdge"/>.
    /// </summary>
    public StrokeShape? PasteImage(byte[] imageBytes, SketchPoint centerWorld, double maxEdge = 320)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        if (imageBytes.Length == 0)
            return null;

        Bitmap bitmap;
        try
        {
            using var ms = new MemoryStream(imageBytes);
            bitmap = new Bitmap(ms);
        }
        catch
        {
            return null;
        }

        maxEdge = Math.Max(16, maxEdge);
        var pw = Math.Max(1.0, bitmap.PixelSize.Width);
        var ph = Math.Max(1.0, bitmap.PixelSize.Height);
        var scale = Math.Min(1.0, maxEdge / Math.Max(pw, ph));
        var w = pw * scale;
        var h = ph * scale;
        var left = centerWorld.X - w * 0.5;
        var top = centerWorld.Y - h * 0.5;

        string b64;
        try
        {
            using var png = new MemoryStream();
            bitmap.Save(png);
            b64 = Convert.ToBase64String(png.ToArray());
        }
        catch
        {
            bitmap.Dispose();
            return null;
        }

        var id = Guid.NewGuid().ToString("N");
        _imageCache[id] = bitmap;

        var stroke = new StrokeShape
        {
            Id = id,
            Kind = SketchElementKind.Image,
            ImagePngBase64 = b64,
            StrokeColor = StrokeColor,
            StrokeWidth = 0,
            Closed = true,
            Points =
            [
                new SketchPoint(left, top),
                new SketchPoint(left + w, top),
                new SketchPoint(left + w, top + h),
                new SketchPoint(left, top + h),
                new SketchPoint(left, top)
            ]
        };

        EnsureDocument().AddStroke(stroke);
        EnsureDocument().Select(stroke.Id);
        RaiseSelectionIfNeeded();
        DocumentChanged?.Invoke();
        InvalidateVisual();
        return stroke;
    }

    /// <summary>World-space point at the center of the current viewport.</summary>
    public SketchPoint ViewportCenterWorld() =>
        ScreenToWorld(new Point(Bounds.Width * 0.5, Bounds.Height * 0.5));

    /// <summary>Undoes the last document mutation.</summary>
    public bool Undo()
    {
        EndTextEdit(commit: true);
        var ok = EnsureDocument().Undo();
        if (ok)
            InvalidateVisual();
        return ok;
    }

    /// <summary>Redoes the last undone mutation.</summary>
    public bool Redo()
    {
        EndTextEdit(commit: true);
        var ok = EnsureDocument().Redo();
        if (ok)
            InvalidateVisual();
        return ok;
    }

    /// <summary>Clears all strokes.</summary>
    public void Clear()
    {
        EndTextEdit(commit: false);
        CancelInProgressDrawing();
        EnsureDocument().Clear();
        ClearImageCache();
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_textEditor is not null)
            return;

        if (e.Key == Key.Space)
        {
            _spaceDown = true;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelInProgressDrawing();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && Tool is SketchTool.Line or SketchTool.Spline
            && (_draft is { Count: > 0 } || _splineControls is { Count: > 0 }))
        {
            CompleteDrawing(closeShape: e.KeyModifiers.HasFlag(KeyModifiers.Control)
                                        || e.KeyModifiers.HasFlag(KeyModifiers.Shift));
            e.Handled = true;
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (ctrl && e.Key == Key.Z)
        {
            Undo();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.Y)
        {
            Redo();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.A && Tool == SketchTool.Select)
        {
            EnsureDocument().SetSelection(EnsureDocument().Elements.Select(x => x.Id));
            RaiseSelectionIfNeeded();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.G)
        {
            if (shift)
                UngroupSelection();
            else
                FuseSelection();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.Delete or Key.Back)
        {
            EnsureDocument().DeleteSelection();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _spaceDown = false;
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var screen = e.GetPosition(this);
        var worldUnderCursor = ScreenToWorld(screen);
        var factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
        _scale = Math.Clamp(_scale * factor, 0.05, 40);
        _offsetX = screen.X - worldUnderCursor.X * _scale;
        _offsetY = screen.Y - worldUnderCursor.Y * _scale;
        InvalidateVisual();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_textEditor is not null)
        {
            EndTextEdit(commit: true);
            InvalidateVisual();
        }

        Focus();
        var screen = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;
        _lastScreen = screen;

        if (props.IsMiddleButtonPressed || (_spaceDown && props.IsLeftButtonPressed))
        {
            _panning = true;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!props.IsLeftButtonPressed)
            return;

        var doc = EnsureDocument();
        var world = SnapPointer(ScreenToWorld(screen));
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        switch (Tool)
        {
            case SketchTool.Pen:
                _draft = [world];
                _dragMode = DragMode.Draw;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;

            case SketchTool.Line:
            case SketchTool.Spline:
                HandleVertexClick(world, e.ClickCount);
                e.Handled = true;
                InvalidateVisual();
                return;

            case SketchTool.Rect:
            case SketchTool.Ellipse:
            case SketchTool.SpeechBubble:
            case SketchTool.TextBox:
                _draft = [world, world];
                _dragMode = DragMode.ShapeDrag;
                _dragStartWorld = world;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;

            case SketchTool.Text:
                PlaceTextLabel(world);
                e.Handled = true;
                return;

            case SketchTool.Eraser:
                _eraserBatch = [];
                EraseAt(world);
                _dragMode = DragMode.Erase;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;

            case SketchTool.Fill:
                ApplyPaintBucket(screen);
                e.Handled = true;
                InvalidateVisual();
                return;
        }

        // Select
        if (TryHitRotateGrip(screen))
        {
            BeginGeometryDrag(doc, DragMode.Rotate, world);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (TryHitGrip(screen, out var grip, out _))
        {
            BeginGeometryDrag(doc, DragMode.Resize, world);
            _activeGrip = grip;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        var hit = HitTestStroke(screen);
        if (hit is not null)
        {
            if (e.ClickCount >= 2 && hit.Kind is SketchElementKind.Text or SketchElementKind.TextBox)
            {
                doc.Select(hit.Id);
                RaiseSelectionIfNeeded();
                BeginTextEdit(hit);
                e.Handled = true;
                InvalidateVisual();
                return;
            }

            if (shift || ctrl)
                doc.ToggleSelection(hit.Id);
            else if (!doc.Selection.Contains(hit.Id))
                doc.Select(hit.Id);

            RaiseSelectionIfNeeded();
            if (doc.Selection.Contains(hit.Id))
                BeginGeometryDrag(doc, DragMode.Move, world);

            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        _dragMode = DragMode.Marquee;
        _marqueeStartScreen = screen;
        _marqueeScreen = new Rect(screen, screen);
        _marqueeAdditive = shift || ctrl;
        if (!_marqueeAdditive)
            doc.Select(null);
        RaiseSelectionIfNeeded();
        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var screen = e.GetPosition(this);
        var raw = ScreenToWorld(screen);
        var world = SnapPointer(raw);
        _hoverWorld = world;

        var skipMeetupHint = _dragMode == DragMode.Draw && Tool == SketchTool.Pen;
        _meetupHint = !skipMeetupHint && MeetupEnabled
            ? SketchMeetup.FindNearestVertex(EnsureDocument().Elements, raw, MeetupWorldRadius())
            : null;

        if (_panning && _lastScreen is { } lastPan)
        {
            _offsetX += screen.X - lastPan.X;
            _offsetY += screen.Y - lastPan.Y;
            _lastScreen = screen;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.Draw && _draft is not null)
        {
            if (_draft.Count == 0 || Distance(_draft[^1], world) >= MinSampleDistance / Math.Max(_scale, 0.01))
                _draft.Add(world);
            _lastScreen = screen;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.ShapeDrag && _draft is { Count: >= 2 })
        {
            _draft = BuildShapeDraft(_dragStartWorld, world, e.KeyModifiers);
            _lastScreen = screen;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.Erase)
        {
            EraseAt(world);
            _lastScreen = screen;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_dragMode == DragMode.Marquee)
        {
            var x = Math.Min(_marqueeStartScreen.X, screen.X);
            var y = Math.Min(_marqueeStartScreen.Y, screen.Y);
            _marqueeScreen = new Rect(x, y, Math.Abs(screen.X - _marqueeStartScreen.X), Math.Abs(screen.Y - _marqueeStartScreen.Y));
            _lastScreen = screen;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if ((_dragMode is DragMode.Move or DragMode.Resize or DragMode.Rotate)
            && _dragOriginalPoints is not null)
        {
            ApplyLiveDrag(world);
            _lastScreen = screen;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (Tool is SketchTool.Line or SketchTool.Spline)
            InvalidateVisual();

        _lastScreen = screen;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragMode == DragMode.Draw && _draft is { Count: > 0 })
        {
            CommitPolyline(SketchPrimitives.SmoothPolyline(_draft, iterations: 1));
        }
        else if (_dragMode == DragMode.ShapeDrag && _draft is { Count: >= 2 })
        {
            if (Tool == SketchTool.TextBox)
                CommitTextBox(_draft);
            else
                CommitPolyline(_draft);
        }
        else if (_dragMode == DragMode.Erase)
        {
            FlushEraserBatch();
        }
        else if (_dragMode == DragMode.Marquee)
        {
            FinishMarquee();
        }
        else if (_dragMode is DragMode.Move or DragMode.Resize or DragMode.Rotate)
        {
            DocumentChanged?.Invoke();
        }

        _dragMode = DragMode.None;
        _dragOriginalPoints = null;
        _dragOriginalRotations = null;
        _dragUnionBounds = null;
        _marqueeScreen = null;
        _eraserBatch = null;
        _panning = false;
        _lastScreen = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
        e.Handled = true;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

        var doc = EnsureDocument();
        if (doc.Grid.Visible)
            DrawGrid(context, doc.Grid.Size);

        foreach (var stroke in doc.Elements)
        {
            if (!doc.IsLayerVisible(stroke.LayerId))
                continue;
            DrawElement(context, stroke);
        }

        var draftPen = CreateStrokePen(
            new ImmutableSolidColorBrush(ParseColor(StrokeColor)),
            Math.Max(0.15, StrokeWidth * _scale),
            StrokeStyle);

        if (_draft is { Count: > 0 } && Tool != SketchTool.TextBox)
            DrawPolyline(context, _draft, draftPen);
        else if (_draft is { Count: >= 2 } && Tool == SketchTool.TextBox)
        {
            var b = SketchBounds.FromPoints(_draft);
            var tl = WorldToScreen(new SketchPoint(b.X, b.Y));
            var br = WorldToScreen(new SketchPoint(b.Right, b.Bottom));
            context.DrawRectangle(null, draftPen,
                new Rect(Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y), Math.Abs(br.X - tl.X), Math.Abs(br.Y - tl.Y)));
        }

        if (Tool == SketchTool.Spline && _splineControls is { Count: > 0 })
        {
            DrawPolyline(context, _splineControls, RubberPen);
            var preview = SketchPrimitives.CatmullRom(_splineControls);
            if (preview.Count > 1)
                DrawPolyline(context, preview, draftPen);
            if (_hoverWorld is { } hover)
                context.DrawLine(RubberPen, WorldToScreen(_splineControls[^1]), WorldToScreen(hover));
        }
        else if (Tool == SketchTool.Line && _draft is { Count: > 0 } && _hoverWorld is { } lineHover)
        {
            context.DrawLine(RubberPen, WorldToScreen(_draft[^1]), WorldToScreen(lineHover));
        }

        if (_marqueeScreen is { } marquee && marquee.Width + marquee.Height > 0)
            context.DrawRectangle(null, MarqueePen, marquee);

        DrawSelections(context, doc);

        if (_meetupHint is { } meet)
            DrawMeetupMarker(context, meet);
        else if (SnapEnabled && _hoverWorld is { } snapPt
                 && Tool is not SketchTool.Select)
            DrawSnapMarker(context, snapPt);
    }

    List<SketchPoint> BuildShapeDraft(SketchPoint start, SketchPoint end, KeyModifiers modifiers)
    {
        var forceCircle = Tool == SketchTool.Ellipse && modifiers.HasFlag(KeyModifiers.Shift);
        return Tool switch
        {
            SketchTool.Ellipse => SketchPrimitives.Ellipse(start, end, forceCircle),
            SketchTool.SpeechBubble => SketchPrimitives.SpeechBubble(start, end),
            SketchTool.TextBox => SketchPrimitives.Rect(start, end),
            _ => SketchPrimitives.Rect(start, end)
        };
    }

    void PlaceTextLabel(SketchPoint world)
    {
        var strokeColor = string.IsNullOrWhiteSpace(StrokeColor) ? "#1e1e1e" : StrokeColor;
        var stroke = new StrokeShape
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = SketchElementKind.Text,
            Text = "Text",
            FontSize = DefaultFontSize,
            StrokeColor = strokeColor,
            StrokeWidth = 0,
            Points = [world, new SketchPoint(world.X + DefaultFontSize * 4, world.Y + DefaultFontSize)]
        };
        EnsureDocument().AddStroke(stroke);
        EnsureDocument().Select(stroke.Id);
        RaiseSelectionIfNeeded();
        DocumentChanged?.Invoke();
        BeginTextEdit(stroke);
        InvalidateVisual();
    }

    void CommitTextBox(IReadOnlyList<SketchPoint> points)
    {
        var b = SketchBounds.FromPoints(points);
        if (b.Width < 4 && b.Height < 4)
        {
            _draft = null;
            return;
        }

        var strokeColor = string.IsNullOrWhiteSpace(StrokeColor) ? "#1e1e1e" : StrokeColor;
        string? fill = null;
        if (FillEnabled)
            fill = string.IsNullOrWhiteSpace(FillColor) ? "#ffffff" : FillColor;

        var stroke = new StrokeShape
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = SketchElementKind.TextBox,
            Text = "Text",
            FontSize = DefaultFontSize,
            StrokeColor = strokeColor,
            StrokeWidth = StrokeWidth < 0.05 ? 2 : StrokeWidth,
            FillColor = fill,
            Closed = true,
            Points =
            [
                new SketchPoint(b.X, b.Y),
                new SketchPoint(b.Right, b.Y),
                new SketchPoint(b.Right, b.Bottom),
                new SketchPoint(b.X, b.Bottom),
                new SketchPoint(b.X, b.Y)
            ]
        };
        EnsureDocument().AddStroke(stroke);
        EnsureDocument().Select(stroke.Id);
        RaiseSelectionIfNeeded();
        DocumentChanged?.Invoke();
        _draft = null;
        BeginTextEdit(stroke);
        InvalidateVisual();
    }

    void BeginTextEdit(StrokeShape element)
    {
        EndTextEdit(commit: true);
        if (element.Kind is not (SketchElementKind.Text or SketchElementKind.TextBox))
            return;

        var bounds = SketchBounds.FromPoints(element.Points);
        var tl = WorldToScreen(new SketchPoint(bounds.X, bounds.Y));
        var br = WorldToScreen(new SketchPoint(bounds.Right, bounds.Bottom));
        var width = Math.Max(80, Math.Abs(br.X - tl.X));
        var height = Math.Max(28, Math.Abs(br.Y - tl.Y));
        var left = Math.Min(tl.X, br.X);
        var top = Math.Min(tl.Y, br.Y);

        _editingElementId = element.Id;
        _textEditor = new TextBox
        {
            Text = element.Text ?? "",
            FontSize = Math.Max(10, element.FontSize * _scale),
            Width = width,
            Height = height,
            AcceptsReturn = element.Kind == SketchElementKind.TextBox,
            Background = Brushes.White,
            BorderBrush = Brushes.DodgerBlue,
            BorderThickness = new Thickness(1)
        };
        _textEditor.KeyDown += OnTextEditorKeyDown;
        _textEditor.LostFocus += OnTextEditorLostFocus;

        _textPopup = new Popup
        {
            Placement = PlacementMode.AnchorAndGravity,
            PlacementTarget = this,
            PlacementRect = new Rect(left, top, width, height),
            PlacementAnchor = global::Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft,
            PlacementGravity = global::Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight,
            Child = _textEditor,
            IsLightDismissEnabled = true,
            IsOpen = true
        };
        _textPopup.Closed += OnTextPopupClosed;
        LogicalChildren.Add(_textPopup);
        VisualChildren.Add(_textPopup);
        _textEditor.Focus();
        _textEditor.SelectAll();
    }

    void OnTextPopupClosed(object? sender, EventArgs e) => EndTextEdit(commit: true);

    void OnTextEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            EndTextEdit(commit: false);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            && _editingElementId is not null
            && EnsureDocument().Find(_editingElementId)?.Kind == SketchElementKind.Text)
        {
            EndTextEdit(commit: true);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    void OnTextEditorLostFocus(object? sender, RoutedEventArgs e) =>
        EndTextEdit(commit: true);

    void EndTextEdit(bool commit)
    {
        if (_endingTextEdit)
            return;
        if (_textEditor is null && _textPopup is null)
            return;

        _endingTextEdit = true;
        try
        {
            var editor = _textEditor;
            var popup = _textPopup;
            var id = _editingElementId;

            if (editor is not null)
            {
                editor.KeyDown -= OnTextEditorKeyDown;
                editor.LostFocus -= OnTextEditorLostFocus;
            }

            if (popup is not null)
            {
                popup.Closed -= OnTextPopupClosed;
                popup.IsOpen = false;
                LogicalChildren.Remove(popup);
                VisualChildren.Remove(popup);
            }

            _textEditor = null;
            _textPopup = null;
            _editingElementId = null;

            if (!commit || id is null || editor is null)
                return;

            var text = editor.Text ?? "";
            var doc = EnsureDocument();
            var el = doc.Find(id);
            if (el is null)
                return;

            if (string.Equals(el.Text, text, StringComparison.Ordinal))
                return;

            doc.Mutate(() => el.Text = text);
            DocumentChanged?.Invoke();
            InvalidateVisual();
        }
        finally
        {
            _endingTextEdit = false;
        }
    }

    void HandleVertexClick(SketchPoint world, int clickCount)
    {
        var now = DateTime.UtcNow;
        var isDouble = clickCount >= 2
                       || (_lastVertexClickWorld is { } prev
                           && (now - _lastVertexClickUtc).TotalMilliseconds < 350
                           && Distance(prev, world) < GridSize * 0.35);
        _lastVertexClickUtc = now;
        _lastVertexClickWorld = world;

        if (isDouble)
        {
            var count = Tool == SketchTool.Spline
                ? _splineControls?.Count ?? 0
                : _draft?.Count ?? 0;
            CompleteDrawing(closeShape: count >= 3);
            return;
        }

        if (Tool == SketchTool.Line)
        {
            _draft ??= [];
            if (_draft.Count >= 3 && Distance(_draft[0], world) <= CloseRadiusWorld())
            {
                CompleteDrawing(closeShape: true);
                return;
            }

            if (_draft.Count == 0 || Distance(_draft[^1], world) > 1e-6)
                _draft.Add(world);
            return;
        }

        _splineControls ??= [];
        if (_splineControls.Count >= 3 && Distance(_splineControls[0], world) <= CloseRadiusWorld())
        {
            CompleteDrawing(closeShape: true);
            return;
        }

        if (_splineControls.Count == 0 || Distance(_splineControls[^1], world) > 1e-6)
            _splineControls.Add(world);
    }

    double CloseRadiusWorld() =>
        MeetupScreenRadius / Math.Max(_scale, 0.01);

    void CommitVertexTool()
    {
        if (Tool == SketchTool.Spline && _splineControls is { Count: > 0 } controls)
        {
            var tess = SketchPrimitives.CatmullRom(controls);
            CommitPolyline(tess.Count >= 2 ? tess : [.. controls]);
            _splineControls = null;
            return;
        }

        if (_draft is { Count: > 0 } draft)
            CommitPolyline(draft);
    }

    void CommitPolyline(IReadOnlyList<SketchPoint> points)
    {
        if (points.Count == 0)
        {
            _pendingClose = false;
            return;
        }

        var pts = points.Count == 1
            ? new List<SketchPoint> { points[0], points[0] }
            : points.ToList();

        var closed = _pendingClose
                     || Tool is SketchTool.Rect or SketchTool.Ellipse or SketchTool.SpeechBubble
                     || IsNearlyClosed(pts);
        _pendingClose = false;

        if (closed && pts.Count >= 3 && Distance(pts[0], pts[^1]) > 1e-6)
            pts.Add(pts[0]);

        var strokeColor = string.IsNullOrWhiteSpace(StrokeColor) ? "#1e1e1e" : StrokeColor;
        string? fill = null;
        if (FillEnabled && closed)
            fill = string.IsNullOrWhiteSpace(FillColor) ? strokeColor : FillColor;

        var stroke = new StrokeShape
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = SketchElementKind.Stroke,
            Points = pts,
            StrokeColor = strokeColor,
            StrokeWidth = StrokeWidth < 0.05 ? 2 : StrokeWidth,
            FillColor = fill,
            StrokeStyle = StrokeStyle,
            Closed = closed
        };
        EnsureDocument().AddStroke(stroke);
        EnsureDocument().Select(stroke.Id);
        RaiseSelectionIfNeeded();
        DocumentChanged?.Invoke();
        _draft = null;
        _splineControls = null;
        _hoverWorld = null;
        _lastVertexClickWorld = null;
        InvalidateVisual();
    }

    static bool IsNearlyClosed(IReadOnlyList<SketchPoint> pts)
    {
        if (pts.Count < 3)
            return false;
        return Distance(pts[0], pts[^1]) < 1e-6;
    }

    void CancelInProgressDrawing()
    {
        _draft = null;
        _splineControls = null;
        _hoverWorld = null;
        _meetupHint = null;
        _lastVertexClickWorld = null;
        _pendingClose = false;
        _dragMode = DragMode.None;
        _marqueeScreen = null;
        _eraserBatch = null;
    }

    void EraseAt(SketchPoint world)
    {
        var tol = HitToleranceScreen / Math.Max(_scale, 0.01);
        var doc = EnsureDocument();
        for (var i = doc.Elements.Count - 1; i >= 0; i--)
        {
            var stroke = doc.Elements[i];
            if (!doc.IsLayerVisible(stroke.LayerId) || doc.IsLayerLocked(stroke.LayerId))
                continue;
            if (HitDistance(stroke, world) <= tol + stroke.StrokeWidth)
                _eraserBatch?.Add(stroke.Id);
        }
    }

    void FlushEraserBatch()
    {
        if (_eraserBatch is not { Count: > 0 } batch)
            return;
        EnsureDocument().DeleteByIds(batch);
        DocumentChanged?.Invoke();
        _eraserBatch = null;
    }

    void FinishMarquee()
    {
        var doc = EnsureDocument();
        if (_marqueeScreen is not { } box)
            return;

        if (box.Width < MarqueeStartSlop && box.Height < MarqueeStartSlop)
        {
            if (!_marqueeAdditive)
                doc.Select(null);
            RaiseSelectionIfNeeded();
            return;
        }

        var worldA = ScreenToWorld(box.TopLeft);
        var worldB = ScreenToWorld(box.BottomRight);
        var left = Math.Min(worldA.X, worldB.X);
        var top = Math.Min(worldA.Y, worldB.Y);
        var right = Math.Max(worldA.X, worldB.X);
        var bottom = Math.Max(worldA.Y, worldB.Y);
        var worldBox = new SketchRect(left, top, right - left, bottom - top);

        var hits = new List<string>();
        foreach (var stroke in doc.Elements)
        {
            if (stroke.Points.Count == 0)
                continue;
            if (RectsIntersect(worldBox, ElementWorldBounds(stroke)))
                hits.Add(stroke.Id);
        }

        if (_marqueeAdditive)
            doc.SetSelection(doc.Selection.Concat(hits).Distinct(StringComparer.Ordinal));
        else
            doc.SetSelection(hits);

        RaiseSelectionIfNeeded();
    }

    static bool RectsIntersect(SketchRect a, SketchRect b) =>
        a.X <= b.Right && a.Right >= b.X && a.Y <= b.Bottom && a.Bottom >= b.Y;

    void BeginGeometryDrag(SketchDocument doc, DragMode mode, SketchPoint world)
    {
        _dragMode = mode;
        _dragStartWorld = world;
        _dragOriginalPoints = new Dictionary<string, List<SketchPoint>>(StringComparer.Ordinal);
        _dragOriginalRotations = new Dictionary<string, double>(StringComparer.Ordinal);

        var targets = doc.Elements
            .Where(e => doc.Selection.Contains(e.Id) && !doc.IsLayerLocked(e.LayerId))
            .ToList();
        if (targets.Count == 0)
            return;

        doc.Checkpoint();
        foreach (var t in targets)
        {
            _dragOriginalPoints[t.Id] = [.. t.Points];
            _dragOriginalRotations[t.Id] = t.RotationDegrees;
        }

        SketchRect? union = null;
        foreach (var t in targets)
        {
            var b = ElementWorldBounds(t);
            union = union is null ? b : Union(union.Value, b);
        }

        _dragUnionBounds = union;
    }

    void ApplyLiveDrag(SketchPoint world)
    {
        var doc = EnsureDocument();
        if (_dragOriginalPoints is null)
            return;

        if (_dragMode == DragMode.Move)
        {
            var dx = world.X - _dragStartWorld.X;
            var dy = world.Y - _dragStartWorld.Y;
            if (SnapEnabled)
            {
                var g = Math.Max(1e-9, GridSize);
                dx = Math.Round(dx / g) * g;
                dy = Math.Round(dy / g) * g;
            }

            foreach (var (id, original) in _dragOriginalPoints)
            {
                var stroke = doc.Find(id);
                if (stroke is null)
                    continue;
                stroke.Points = original.Select(p => new SketchPoint(p.X + dx, p.Y + dy)).ToList();
            }

            return;
        }

        if (_dragMode == DragMode.Rotate && _dragUnionBounds is { } rotUnion && _dragOriginalRotations is not null)
        {
            var center = rotUnion.Center;
            var startAngle = Math.Atan2(_dragStartWorld.Y - center.Y, _dragStartWorld.X - center.X);
            var curAngle = Math.Atan2(world.Y - center.Y, world.X - center.X);
            var deltaDeg = (curAngle - startAngle) * 180.0 / Math.PI;
            foreach (var (id, _) in _dragOriginalPoints)
            {
                var stroke = doc.Find(id);
                if (stroke is null)
                    continue;
                stroke.RotationDegrees = _dragOriginalRotations[id] + deltaDeg;
            }

            return;
        }

        if (_dragUnionBounds is not { } oldUnion)
            return;

        var newBounds = ResizeBounds(oldUnion, _activeGrip, world);
        if (SnapEnabled)
            newBounds = SnapRect(newBounds);

        foreach (var (id, original) in _dragOriginalPoints)
        {
            var stroke = doc.Find(id);
            if (stroke is null)
                continue;
            stroke.Points = [.. original];
            SketchBounds.ApplyBoundsTransform(stroke.Points, oldUnion, newBounds);
        }
    }

    SketchRect ResizeBounds(SketchRect old, GripKind grip, SketchPoint cursor)
    {
        var left = old.X;
        var top = old.Y;
        var right = old.Right;
        var bottom = old.Bottom;

        switch (grip)
        {
            case GripKind.NW: left = cursor.X; top = cursor.Y; break;
            case GripKind.N: top = cursor.Y; break;
            case GripKind.NE: right = cursor.X; top = cursor.Y; break;
            case GripKind.E: right = cursor.X; break;
            case GripKind.SE: right = cursor.X; bottom = cursor.Y; break;
            case GripKind.S: bottom = cursor.Y; break;
            case GripKind.SW: left = cursor.X; bottom = cursor.Y; break;
            case GripKind.W: left = cursor.X; break;
        }

        if (right < left) (left, right) = (right, left);
        if (bottom < top) (top, bottom) = (bottom, top);
        return new SketchRect(left, top, Math.Max(1e-6, right - left), Math.Max(1e-6, bottom - top));
    }

    SketchRect SnapRect(SketchRect r)
    {
        var tl = SketchSnap.Snap(new SketchPoint(r.X, r.Y), GridSize);
        var br = SketchSnap.Snap(new SketchPoint(r.Right, r.Bottom), GridSize);
        return new SketchRect(tl.X, tl.Y, Math.Max(GridSize, br.X - tl.X), Math.Max(GridSize, br.Y - tl.Y));
    }

    bool TryHitRotateGrip(Point screen)
    {
        var union = SelectionUnion(EnsureDocument());
        if (union is null)
            return false;
        var gp = RotateGripScreen(union.Value);
        return Distance(gp, screen) <= GripSizeScreen + 4;
    }

    bool TryHitGrip(Point screen, out GripKind grip, out StrokeShape? stroke)
    {
        grip = default;
        stroke = null;
        var doc = EnsureDocument();
        var union = SelectionUnion(doc);
        if (union is null)
            return false;

        foreach (var kind in AllGrips)
        {
            var gp = WorldToScreen(GripWorld(union.Value, kind));
            if (Distance(gp, screen) <= GripSizeScreen)
            {
                grip = kind;
                stroke = doc.Elements.FirstOrDefault(e => doc.Selection.Contains(e.Id));
                return stroke is not null;
            }
        }

        return false;
    }

    StrokeShape? HitTestStroke(Point screen)
    {
        var doc = EnsureDocument();
        var tol = HitToleranceScreen / Math.Max(_scale, 0.01);
        var world = ScreenToWorld(screen);
        for (var i = doc.Elements.Count - 1; i >= 0; i--)
        {
            var stroke = doc.Elements[i];
            if (!doc.IsLayerVisible(stroke.LayerId))
                continue;
            if (HitDistance(stroke, world) <= tol)
                return stroke;
        }

        return null;
    }

    static double HitDistance(StrokeShape stroke, SketchPoint world)
    {
        if (stroke.Points.Count == 0)
            return double.PositiveInfinity;

        if (stroke.Kind is SketchElementKind.Text or SketchElementKind.TextBox or SketchElementKind.Image)
            return SketchBounds.HitRotatedRect(stroke.Points, stroke.RotationDegrees, world)
                ? 0
                : double.PositiveInfinity;

        var closed = stroke.Closed || IsNearlyClosed(stroke.Points);
        if (closed && stroke.Points.Count >= 3
            && SketchBounds.PointInRotatedPolygon(stroke.Points, stroke.RotationDegrees, world))
            return 0;

        return SketchBounds.DistanceToRotatedPolyline(stroke.Points, stroke.RotationDegrees, world);
    }

    void ApplyPaintBucket(Point screen)
    {
        var fill = string.IsNullOrWhiteSpace(FillColor)
            ? (string.IsNullOrWhiteSpace(StrokeColor) ? "#1e1e1e" : StrokeColor)
            : FillColor;
        var world = ScreenToWorld(screen);
        var hit = HitTestStroke(screen);
        if (hit is not null && EnsureDocument().ApplyFill(hit.Id, fill))
        {
            DocumentChanged?.Invoke();
            RaiseSelectionIfNeeded();
            return;
        }

        if (EnsureDocument().TryFloodFill(world, fill))
        {
            DocumentChanged?.Invoke();
            RaiseSelectionIfNeeded();
        }
    }

    static SketchRect ElementWorldBounds(StrokeShape stroke) =>
        SketchBounds.RotatedAabb(stroke.Points, stroke.RotationDegrees);

    void DrawGrid(DrawingContext context, double step)
    {
        step = Math.Max(1, step);
        var topLeft = ScreenToWorld(new Point(0, 0));
        var bottomRight = ScreenToWorld(new Point(Bounds.Width, Bounds.Height));
        var minX = Math.Min(topLeft.X, bottomRight.X);
        var maxX = Math.Max(topLeft.X, bottomRight.X);
        var minY = Math.Min(topLeft.Y, bottomRight.Y);
        var maxY = Math.Max(topLeft.Y, bottomRight.Y);
        var startX = Math.Floor(minX / step) * step;
        var startY = Math.Floor(minY / step) * step;

        for (var x = startX; x <= maxX + step; x += step)
        {
            var major = Math.Abs(Math.Round(x / step) % 5) < 1e-9;
            context.DrawLine(major ? GridMajorPen : GridPen,
                WorldToScreen(new SketchPoint(x, minY)), WorldToScreen(new SketchPoint(x, maxY)));
        }

        for (var y = startY; y <= maxY + step; y += step)
        {
            var major = Math.Abs(Math.Round(y / step) % 5) < 1e-9;
            context.DrawLine(major ? GridMajorPen : GridPen,
                WorldToScreen(new SketchPoint(minX, y)), WorldToScreen(new SketchPoint(maxX, y)));
        }
    }

    void DrawElement(DrawingContext context, StrokeShape stroke)
    {
        if (stroke.Points.Count == 0)
            return;

        if (stroke.Kind == SketchElementKind.Image)
        {
            DrawImageElement(context, stroke);
            return;
        }

        if (stroke.Kind is SketchElementKind.Text or SketchElementKind.TextBox)
        {
            DrawTextElement(context, stroke);
            return;
        }

        var thickness = Math.Max(0.15, stroke.StrokeWidth * _scale);
        var pen = CreateStrokePen(
            new ImmutableSolidColorBrush(ParseColor(stroke.StrokeColor)),
            thickness,
            stroke.StrokeStyle);
        IBrush? fill = null;
        if (!string.IsNullOrWhiteSpace(stroke.FillColor) && (stroke.Closed || IsNearlyClosed(stroke.Points)))
            fill = new ImmutableSolidColorBrush(ParseColor(stroke.FillColor));

        var center = SketchBounds.LocalCenter(stroke.Points);
        using (context.PushTransform(BuildWorldRotation(center, stroke.RotationDegrees)))
            DrawPolyline(context, stroke.Points, pen, fill, stroke.Closed || IsNearlyClosed(stroke.Points));
    }

    void DrawTextElement(DrawingContext context, StrokeShape stroke)
    {
        var bounds = SketchBounds.FromPoints(stroke.Points);
        var center = bounds.Center;
        using (context.PushTransform(BuildWorldRotation(center, stroke.RotationDegrees)))
        {
            if (stroke.Kind == SketchElementKind.TextBox)
            {
                var thickness = Math.Max(0.15, stroke.StrokeWidth * _scale);
                var pen = CreateStrokePen(
                    new ImmutableSolidColorBrush(ParseColor(stroke.StrokeColor)),
                    thickness,
                    stroke.StrokeStyle);
                IBrush? fill = null;
                if (!string.IsNullOrWhiteSpace(stroke.FillColor))
                    fill = new ImmutableSolidColorBrush(ParseColor(stroke.FillColor));
                var tl = WorldToScreen(new SketchPoint(bounds.X, bounds.Y));
                var br = WorldToScreen(new SketchPoint(bounds.Right, bounds.Bottom));
                var rect = new Rect(Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y), Math.Abs(br.X - tl.X), Math.Abs(br.Y - tl.Y));
                context.DrawRectangle(fill, pen, rect);
            }

            if (_editingElementId == stroke.Id)
                return;

            var text = string.IsNullOrEmpty(stroke.Text) ? " " : stroke.Text;
            var fontSize = Math.Max(8, stroke.FontSize * _scale);
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                fontSize,
                new ImmutableSolidColorBrush(ParseColor(stroke.StrokeColor)));

            var origin = stroke.Kind == SketchElementKind.Text
                ? WorldToScreen(stroke.Points[0])
                : WorldToScreen(new SketchPoint(bounds.X + 4, bounds.Y + 4));
            context.DrawText(ft, origin);
        }
    }

    void DrawImageElement(DrawingContext context, StrokeShape stroke)
    {
        var bitmap = GetOrLoadBitmap(stroke);
        if (bitmap is null)
            return;

        var bounds = SketchBounds.FromPoints(stroke.Points);
        var center = bounds.Center;
        using (context.PushTransform(BuildWorldRotation(center, stroke.RotationDegrees)))
        {
            var tl = WorldToScreen(new SketchPoint(bounds.X, bounds.Y));
            var br = WorldToScreen(new SketchPoint(bounds.Right, bounds.Bottom));
            var rect = new Rect(Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y), Math.Abs(br.X - tl.X), Math.Abs(br.Y - tl.Y));
            context.DrawImage(bitmap, rect);
        }
    }

    Matrix BuildWorldRotation(SketchPoint worldCenter, double degrees)
    {
        if (Math.Abs(degrees) < 1e-12)
            return Matrix.Identity;
        var c = WorldToScreen(worldCenter);
        return Matrix.CreateTranslation(-c.X, -c.Y)
               * Matrix.CreateRotation(degrees * Math.PI / 180.0)
               * Matrix.CreateTranslation(c.X, c.Y);
    }

    Bitmap? GetOrLoadBitmap(StrokeShape stroke)
    {
        if (_imageCache.TryGetValue(stroke.Id, out var cached))
            return cached;
        if (string.IsNullOrWhiteSpace(stroke.ImagePngBase64))
            return null;
        try
        {
            var bytes = Convert.FromBase64String(stroke.ImagePngBase64);
            using var ms = new MemoryStream(bytes);
            var bmp = new Bitmap(ms);
            _imageCache[stroke.Id] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    void ClearImageCache()
    {
        foreach (var bmp in _imageCache.Values)
            bmp.Dispose();
        _imageCache.Clear();
    }

    void DrawPolyline(
        DrawingContext context,
        IReadOnlyList<SketchPoint> points,
        IPen pen,
        IBrush? fill = null,
        bool closed = false)
    {
        if (points.Count == 0)
            return;

        if (points.Count == 1)
        {
            var r = Math.Max(0.5, pen.Thickness * 0.5);
            context.DrawEllipse(fill ?? pen.Brush, fill is null ? null : pen, WorldToScreen(points[0]), r, r);
            return;
        }

        var list = new List<Point>(points.Count + 1);
        for (var i = 0; i < points.Count; i++)
            list.Add(WorldToScreen(points[i]));
        if (closed && list.Count >= 3)
        {
            var a = list[0];
            var b = list[^1];
            if (Math.Abs(a.X - b.X) > 0.5 || Math.Abs(a.Y - b.Y) > 0.5)
                list.Add(a);
        }

        context.DrawGeometry(fill, pen, new PolylineGeometry(list, isFilled: fill is not null));
    }

    static IPen CreateStrokePen(IImmutableBrush brush, double thickness, SketchStrokeStyle style = SketchStrokeStyle.Solid) =>
        new ImmutablePen(
            brush,
            thickness,
            dashStyle: SketchStrokeStyles.CreateDash(style, thickness),
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);

    void DrawSelections(DrawingContext context, SketchDocument doc)
    {
        var union = SelectionUnion(doc);
        if (union is null)
            return;

        foreach (var id in doc.Selection)
        {
            var stroke = doc.Find(id);
            if (stroke is null || stroke.Points.Count == 0)
                continue;
            DrawSelectionRect(context, ElementWorldBounds(stroke), withGrips: false);
        }

        DrawSelectionRect(context, union.Value, withGrips: true);
        var rot = RotateGripScreen(union.Value);
        context.DrawLine(SelectionPen,
            WorldToScreen(new SketchPoint(union.Value.X + union.Value.Width * 0.5, union.Value.Y)),
            rot);
        context.DrawEllipse(GripBrush, GripPen, rot, 5, 5);
    }

    void DrawSelectionRect(DrawingContext context, SketchRect bounds, bool withGrips)
    {
        var tl = WorldToScreen(new SketchPoint(bounds.X, bounds.Y));
        var br = WorldToScreen(new SketchPoint(bounds.Right, bounds.Bottom));
        var rect = new Rect(Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y), Math.Abs(br.X - tl.X), Math.Abs(br.Y - tl.Y));
        context.DrawRectangle(null, SelectionPen, rect);
        if (!withGrips)
            return;
        foreach (var kind in AllGrips)
        {
            var g = WorldToScreen(GripWorld(bounds, kind));
            context.DrawRectangle(GripBrush, GripPen, new Rect(g.X - 3.5, g.Y - 3.5, 7, 7));
        }
    }

    Point RotateGripScreen(SketchRect union)
    {
        var topMid = WorldToScreen(new SketchPoint(union.X + union.Width * 0.5, union.Y));
        return new Point(topMid.X, topMid.Y - RotateGripOffsetScreen);
    }

    void DrawSnapMarker(DrawingContext context, SketchPoint world)
    {
        var p = WorldToScreen(world);
        context.DrawLine(SelectionPen, new Point(p.X - 6, p.Y), new Point(p.X + 6, p.Y));
        context.DrawLine(SelectionPen, new Point(p.X, p.Y - 6), new Point(p.X, p.Y + 6));
    }

    void DrawMeetupMarker(DrawingContext context, SketchPoint world)
    {
        var p = WorldToScreen(world);
        context.DrawEllipse(null, MeetupPen, p, 7, 7);
        context.DrawLine(MeetupPen, new Point(p.X - 8, p.Y), new Point(p.X + 8, p.Y));
        context.DrawLine(MeetupPen, new Point(p.X, p.Y - 8), new Point(p.X, p.Y + 8));
    }

    SketchRect? SelectionUnion(SketchDocument doc)
    {
        SketchRect? union = null;
        foreach (var id in doc.Selection)
        {
            var stroke = doc.Find(id);
            if (stroke is null || stroke.Points.Count == 0)
                continue;
            var b = ElementWorldBounds(stroke);
            union = union is null ? b : Union(union.Value, b);
        }

        return union;
    }

    static SketchRect Union(SketchRect a, SketchRect b)
    {
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        var right = Math.Max(a.Right, b.Right);
        var bottom = Math.Max(a.Bottom, b.Bottom);
        return new SketchRect(left, top, right - left, bottom - top);
    }

    static SketchPoint GripWorld(SketchRect b, GripKind kind) => kind switch
    {
        GripKind.NW => new SketchPoint(b.X, b.Y),
        GripKind.N => new SketchPoint(b.X + b.Width * 0.5, b.Y),
        GripKind.NE => new SketchPoint(b.Right, b.Y),
        GripKind.E => new SketchPoint(b.Right, b.Y + b.Height * 0.5),
        GripKind.SE => new SketchPoint(b.Right, b.Bottom),
        GripKind.S => new SketchPoint(b.X + b.Width * 0.5, b.Bottom),
        GripKind.SW => new SketchPoint(b.X, b.Bottom),
        GripKind.W => new SketchPoint(b.X, b.Y + b.Height * 0.5),
        _ => b.Center
    };

    static readonly GripKind[] AllGrips =
    [
        GripKind.NW, GripKind.N, GripKind.NE, GripKind.E,
        GripKind.SE, GripKind.S, GripKind.SW, GripKind.W
    ];

    SketchPoint SnapPointer(SketchPoint raw)
    {
        var p = SnapEnabled ? SketchSnap.Snap(raw, GridSize) : raw;
        // Freehand pen must not be yanked onto existing vertices mid-stroke.
        if (_dragMode == DragMode.Draw && Tool == SketchTool.Pen)
            return p;
        if (!MeetupEnabled)
            return p;
        var meet = SketchMeetup.FindNearestVertex(EnsureDocument().Elements, raw, MeetupWorldRadius());
        return meet ?? p;
    }

    double MeetupWorldRadius() => MeetupScreenRadius / Math.Max(_scale, 0.01);

    SketchPoint ScreenToWorld(Point screen) =>
        new((screen.X - _offsetX) / _scale, (screen.Y - _offsetY) / _scale);

    Point WorldToScreen(SketchPoint world) =>
        new(world.X * _scale + _offsetX, world.Y * _scale + _offsetY);

    static double Distance(SketchPoint a, SketchPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    static Color ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return Color.Parse("#1e1e1e");
        try { return Color.Parse(hex); }
        catch { return Color.Parse("#1e1e1e"); }
    }

    SketchDocument EnsureDocument() => Document ?? _document;

    void OnDocumentChanged(AvaloniaPropertyChangedEventArgs e)
    {
        EndTextEdit(commit: false);
        ClearImageCache();
        if (e.OldValue is SketchDocument oldDoc)
            oldDoc.Changed -= OnDocumentMutated;
        if (e.NewValue is SketchDocument newDoc)
        {
            _document = newDoc;
            AttachDocument(newDoc);
            SyncPropertiesFromGrid();
        }
        else if (e.NewValue is null)
        {
            _document = new SketchDocument();
            AttachDocument(_document);
        }

        InvalidateVisual();
    }

    void AttachDocument(SketchDocument doc)
    {
        doc.Changed -= OnDocumentMutated;
        doc.Changed += OnDocumentMutated;
        SyncPropertiesFromGrid();
        _lastSelectionKey = SelectionKey(doc);
    }

    void OnDocumentMutated()
    {
        SyncPropertiesFromGrid();
        RaiseSelectionIfNeeded();
        DocumentChanged?.Invoke();
        InvalidateVisual();
    }

    void SyncGridFromProperties()
    {
        var doc = EnsureDocument();
        doc.Grid.Size = GridSize;
        doc.Grid.Visible = GridVisible;
        doc.Grid.SnapEnabled = SnapEnabled;
        InvalidateVisual();
    }

    void SyncPropertiesFromGrid()
    {
        var doc = EnsureDocument();
        if (Math.Abs(GridSize - doc.Grid.Size) > 1e-9)
            SetCurrentValue(GridSizeProperty, doc.Grid.Size);
        if (GridVisible != doc.Grid.Visible)
            SetCurrentValue(GridVisibleProperty, doc.Grid.Visible);
        if (SnapEnabled != doc.Grid.SnapEnabled)
            SetCurrentValue(SnapEnabledProperty, doc.Grid.SnapEnabled);
    }

    void RaiseSelectionIfNeeded()
    {
        var key = SelectionKey(EnsureDocument());
        if (key == _lastSelectionKey)
            return;
        _lastSelectionKey = key;
        SelectionChanged?.Invoke();
    }

    static string SelectionKey(SketchDocument doc) =>
        string.Join('\u001f', doc.Selection.OrderBy(x => x, StringComparer.Ordinal));

    enum DragMode
    {
        None,
        Draw,
        ShapeDrag,
        Move,
        Resize,
        Rotate,
        Marquee,
        Erase
    }

    enum GripKind
    {
        NW, N, NE, E, SE, S, SW, W
    }
}
