using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Novolis.Avalonia.StarMap;

/// <summary>A plotted star on the map (map units, typically light-years on XZ).</summary>
public sealed class StarMapPoint
{
    /// <summary>Stable id.</summary>
    public required string Id { get; init; }

    /// <summary>Display label.</summary>
    public string? Label { get; init; }

    /// <summary>X map coordinate.</summary>
    public double X { get; init; }

    /// <summary>Y map coordinate (often catalog Z).</summary>
    public double Y { get; init; }
}

/// <summary>A route edge between two point ids.</summary>
public sealed class StarMapEdge
{
    /// <summary>From id.</summary>
    public required string FromId { get; init; }

    /// <summary>To id.</summary>
    public required string ToId { get; init; }

    /// <summary>Optional stroke hint.</summary>
    public string? BandTag { get; init; }
}

/// <summary>Pan/zoom star field with optional route edges and path highlight.</summary>
public sealed class StarMapControl : Control
{
    static readonly IBrush DefaultFieldBrush = new SolidColorBrush(Color.Parse("#0b1020"));
    static readonly IBrush StarBrush = new SolidColorBrush(Color.Parse("#e8e8e8"));
    static readonly IBrush SelectedBrush = new SolidColorBrush(Color.Parse("#ffd166"));
    static readonly IBrush RouteHubBrush = new SolidColorBrush(Color.Parse("#6ecf8e"));
    static readonly IBrush ShipBrush = new SolidColorBrush(Color.Parse("#ff6b35"));
    static readonly IBrush GridBrush = new SolidColorBrush(Color.FromArgb(28, 212, 160, 23));
    static readonly IPen EdgePen = new Pen(new SolidColorBrush(Color.Parse("#3a4a66")), 1.2);
    static readonly IPen RoutePen = new Pen(new SolidColorBrush(Color.Parse("#ffd166")), 2.6);
    static readonly IPen ShipRingPen = new Pen(new SolidColorBrush(Color.Parse("#ffb088")), 1.5);

    /// <summary>Chart field fill (game UIs may warm this without forking render).</summary>
    public IBrush FieldBrush { get; set; } = DefaultFieldBrush;

    /// <summary>Faint chart grid behind stars.</summary>
    public bool ShowChartGrid { get; set; }

    Point? _lastPointer;
    double _offsetX;
    double _offsetY;
    double _scale = 8;

    /// <summary>Stars to draw.</summary>
    public static readonly StyledProperty<IReadOnlyList<StarMapPoint>?> PointsProperty =
        AvaloniaProperty.Register<StarMapControl, IReadOnlyList<StarMapPoint>?>(nameof(Points));

    /// <summary>Route edges to draw (full network).</summary>
    public static readonly StyledProperty<IReadOnlyList<StarMapEdge>?> EdgesProperty =
        AvaloniaProperty.Register<StarMapControl, IReadOnlyList<StarMapEdge>?>(nameof(Edges));

    /// <summary>Highlighted path edges (drawn on top of the network).</summary>
    public static readonly StyledProperty<IReadOnlyList<StarMapEdge>?> HighlightedEdgesProperty =
        AvaloniaProperty.Register<StarMapControl, IReadOnlyList<StarMapEdge>?>(nameof(HighlightedEdges));

    /// <summary>Selected star id.</summary>
    public static readonly StyledProperty<string?> SelectedIdProperty =
        AvaloniaProperty.Register<StarMapControl, string?>(nameof(SelectedId));

    /// <summary>Ship world X (map units).</summary>
    public static readonly StyledProperty<double> ShipWorldXProperty =
        AvaloniaProperty.Register<StarMapControl, double>(nameof(ShipWorldX));

    /// <summary>Ship world Y (map units).</summary>
    public static readonly StyledProperty<double> ShipWorldYProperty =
        AvaloniaProperty.Register<StarMapControl, double>(nameof(ShipWorldY));

    /// <summary>Whether to draw the ship marker.</summary>
    public static readonly StyledProperty<bool> ShipVisibleProperty =
        AvaloniaProperty.Register<StarMapControl, bool>(nameof(ShipVisible));

    static StarMapControl()
    {
        AffectsRender<StarMapControl>(
            PointsProperty, EdgesProperty, HighlightedEdgesProperty, SelectedIdProperty,
            ShipWorldXProperty, ShipWorldYProperty, ShipVisibleProperty);
    }

    /// <summary>Creates the control.</summary>
    public StarMapControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    /// <summary>Stars to draw.</summary>
    public IReadOnlyList<StarMapPoint>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>Route edges.</summary>
    public IReadOnlyList<StarMapEdge>? Edges
    {
        get => GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    /// <summary>Highlighted path edges.</summary>
    public IReadOnlyList<StarMapEdge>? HighlightedEdges
    {
        get => GetValue(HighlightedEdgesProperty);
        set => SetValue(HighlightedEdgesProperty, value);
    }

    /// <summary>Selected star id.</summary>
    public string? SelectedId
    {
        get => GetValue(SelectedIdProperty);
        set => SetValue(SelectedIdProperty, value);
    }

    /// <summary>Ship world X.</summary>
    public double ShipWorldX
    {
        get => GetValue(ShipWorldXProperty);
        set => SetValue(ShipWorldXProperty, value);
    }

    /// <summary>Ship world Y.</summary>
    public double ShipWorldY
    {
        get => GetValue(ShipWorldYProperty);
        set => SetValue(ShipWorldYProperty, value);
    }

    /// <summary>Draw you-are-here ship marker.</summary>
    public bool ShipVisible
    {
        get => GetValue(ShipVisibleProperty);
        set => SetValue(ShipVisibleProperty, value);
    }

    /// <summary>Raised when the user selects a star.</summary>
    public event Action<string>? StarSelected;

    /// <summary>Sets points and edges and invalidates.</summary>
    public void SetMap(IReadOnlyList<StarMapPoint> points, IReadOnlyList<StarMapEdge>? edges = null)
    {
        Points = points;
        Edges = edges;
        InvalidateVisual();
    }

    /// <summary>Sets or clears the highlighted route path.</summary>
    public void SetRoute(IReadOnlyList<StarMapEdge>? routeEdges)
    {
        HighlightedEdges = routeEdges is { Count: > 0 } ? routeEdges : null;
        InvalidateVisual();
    }

    /// <summary>Sets the you-are-here ship marker in world map units.</summary>
    public void SetShipMarker(double worldX, double worldY, bool visible = true)
    {
        ShipWorldX = worldX;
        ShipWorldY = worldY;
        ShipVisible = visible;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        var factor = delta > 0 ? 1.1 : 1 / 1.1;
        _scale = Math.Clamp(_scale * factor, 0.5, 200);
        InvalidateVisual();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pt = e.GetPosition(this);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var hit = HitTest(pt);
            if (hit is not null)
            {
                SelectedId = hit;
                StarSelected?.Invoke(hit);
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            _lastPointer = pt;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_lastPointer is { } last && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var pt = e.GetPosition(this);
            _offsetX += pt.X - last.X;
            _offsetY += pt.Y - last.Y;
            _lastPointer = pt;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _lastPointer = null;
        e.Pointer.Capture(null);
    }

    string? HitTest(Point screen)
    {
        if (Points is null)
            return null;
        foreach (var p in Points)
        {
            var s = WorldToScreen(p.X, p.Y);
            var dx = s.X - screen.X;
            var dy = s.Y - screen.Y;
            if (dx * dx + dy * dy <= 64)
                return p.Id;
        }

        return null;
    }

    Point WorldToScreen(double x, double y) =>
        new(Bounds.Width / 2 + x * _scale + _offsetX, Bounds.Height / 2 - y * _scale + _offsetY);

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(FieldBrush, new Rect(Bounds.Size));

        if (ShowChartGrid && Bounds.Width > 0 && Bounds.Height > 0)
        {
            const double step = 48;
            for (var x = 0.0; x < Bounds.Width; x += step)
            {
                context.FillRectangle(GridBrush, new Rect(x, 0, 1, Bounds.Height));
            }

            for (var y = 0.0; y < Bounds.Height; y += step)
            {
                context.FillRectangle(GridBrush, new Rect(0, y, Bounds.Width, 1));
            }
        }

        var points = Points;
        if (points is null || points.Count == 0)
            return;

        var byId = points.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var routeHubs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (HighlightedEdges is { Count: > 0 } route)
        {
            foreach (var edge in route)
            {
                routeHubs.Add(edge.FromId);
                routeHubs.Add(edge.ToId);
            }
        }

        if (Edges is { Count: > 0 })
        {
            foreach (var edge in Edges)
            {
                if (!byId.TryGetValue(edge.FromId, out var a) || !byId.TryGetValue(edge.ToId, out var b))
                    continue;
                context.DrawLine(EdgePen, WorldToScreen(a.X, a.Y), WorldToScreen(b.X, b.Y));
            }
        }

        if (HighlightedEdges is { Count: > 0 })
        {
            foreach (var edge in HighlightedEdges)
            {
                if (!byId.TryGetValue(edge.FromId, out var a) || !byId.TryGetValue(edge.ToId, out var b))
                    continue;
                context.DrawLine(RoutePen, WorldToScreen(a.X, a.Y), WorldToScreen(b.X, b.Y));
            }
        }

        foreach (var p in points)
        {
            var s = WorldToScreen(p.X, p.Y);
            var selected = string.Equals(p.Id, SelectedId, StringComparison.OrdinalIgnoreCase);
            var onRoute = routeHubs.Contains(p.Id);
            var brush = selected ? SelectedBrush : onRoute ? RouteHubBrush : StarBrush;
            var r = selected ? 5.0 : onRoute ? 4.0 : 3.0;
            context.DrawEllipse(brush, null, s, r, r);
        }

        if (ShipVisible)
        {
            var shipScreen = WorldToScreen(ShipWorldX, ShipWorldY);
            context.DrawEllipse(null, ShipRingPen, shipScreen, 8, 8);
            context.DrawEllipse(ShipBrush, null, shipScreen, 4.5, 4.5);
        }
    }
}
