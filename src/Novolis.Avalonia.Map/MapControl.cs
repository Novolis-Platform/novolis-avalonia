using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>
/// Provider-neutral geographic map control with pan, zoom, selection, markers, circles, tiles, and attribution.
/// </summary>
public sealed class MapControl : Control
{
    static readonly IBrush DefaultBackgroundBrush = new SolidColorBrush(Color.Parse("#e7eef2"));
    static readonly IBrush DefaultGridBrush = new SolidColorBrush(Color.FromArgb(48, 75, 105, 120));
    static readonly IBrush DefaultMarkerBrush = new SolidColorBrush(Color.Parse("#0f6b78"));
    static readonly IBrush DefaultSelectedMarkerBrush = new SolidColorBrush(Color.Parse("#c77b30"));
    static readonly IBrush DefaultSelectedBrush = new SolidColorBrush(Color.Parse("#d28b38"));
    static readonly IBrush DefaultLabelBrush = new SolidColorBrush(Color.Parse("#18333d"));
    static readonly IBrush DefaultAttributionBackground = new SolidColorBrush(Color.FromArgb(210, 245, 248, 249));
    static readonly IPen DefaultCirclePen = new Pen(new SolidColorBrush(Color.Parse("#c77b30")), 2);
    static readonly IBrush DefaultCircleFill = new SolidColorBrush(Color.FromArgb(40, 199, 123, 48));
    static readonly IPen DefaultSelectedPen = new Pen(new SolidColorBrush(Color.Parse("#f4c37a")), 2);
    static readonly IPen DefaultTrackPen = new Pen(new SolidColorBrush(Color.Parse("#0f6b78")), 3);

    readonly Dictionary<MapTileKey, MapTile> _tiles = new();
    CancellationTokenSource? _tileRefreshCancellation;
    bool _isAttached;
    bool _tileRefreshQueued;
    Point? _pointerDown;
    Point? _lastPointer;
    bool _isPanning;

    /// <summary>Viewport state.</summary>
    public static readonly StyledProperty<MapViewport> ViewportProperty =
        AvaloniaProperty.Register<MapControl, MapViewport>(
            nameof(Viewport),
            new MapViewport(new GeoCoordinate(0, 0), 2));

    /// <summary>Markers rendered over the map.</summary>
    public static readonly StyledProperty<IReadOnlyList<MapMarker>?> MarkersProperty =
        AvaloniaProperty.Register<MapControl, IReadOnlyList<MapMarker>?>(nameof(Markers));

    /// <summary>Circles rendered over the map.</summary>
    public static readonly StyledProperty<IReadOnlyList<MapCircleOverlay>?> CirclesProperty =
        AvaloniaProperty.Register<MapControl, IReadOnlyList<MapCircleOverlay>?>(nameof(Circles));

    /// <summary>Connected geographic tracks rendered below markers.</summary>
    public static readonly StyledProperty<IReadOnlyList<MapTrackOverlay>?> TracksProperty =
        AvaloniaProperty.Register<MapControl, IReadOnlyList<MapTrackOverlay>?>(nameof(Tracks));

    /// <summary>Selected geographic coordinate, if any.</summary>
    public static readonly StyledProperty<GeoCoordinate?> SelectedCoordinateProperty =
        AvaloniaProperty.Register<MapControl, GeoCoordinate?>(nameof(SelectedCoordinate));

    /// <summary>Optional provider attribution shown in the lower-left corner.</summary>
    public static readonly StyledProperty<string?> AttributionProperty =
        AvaloniaProperty.Register<MapControl, string?>(nameof(Attribution));

    /// <summary>Optional asynchronous tile provider.</summary>
    public static readonly StyledProperty<IMapTileSource?> TileSourceProperty =
        AvaloniaProperty.Register<MapControl, IMapTileSource?>(nameof(TileSource));

    /// <summary>Whether visible tiles are currently being loaded.</summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<MapControl, bool>(nameof(IsLoading));

    /// <summary>Recoverable provider error displayed over the map.</summary>
    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<MapControl, string?>(nameof(ErrorMessage));

    static MapControl()
    {
        AffectsRender<MapControl>(
            ViewportProperty,
            MarkersProperty,
            CirclesProperty,
            TracksProperty,
            SelectedCoordinateProperty,
            AttributionProperty,
            TileSourceProperty,
            IsLoadingProperty,
            ErrorMessageProperty);
    }

    /// <summary>Creates a map control.</summary>
    public MapControl()
    {
        ClipToBounds = true;
        Focusable = true;
        AttachedToVisualTree += (_, _) =>
        {
            _isAttached = true;
            QueueTileRefresh();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _isAttached = false;
            _tileRefreshCancellation?.Cancel();
        };
        SizeChanged += (_, _) => QueueTileRefresh();
    }

    /// <summary>Map background used underneath raster tiles.</summary>
    public IBrush BackgroundBrush { get; set; } = DefaultBackgroundBrush;

    /// <summary>Faint grid shown when no tile covers a portion of the viewport.</summary>
    public IBrush GridBrush { get; set; } = DefaultGridBrush;

    /// <summary>Default marker brush.</summary>
    public IBrush MarkerBrush { get; set; } = DefaultMarkerBrush;

    /// <summary>Brush for the selected marker.</summary>
    public IBrush SelectedMarkerBrush { get; set; } = DefaultSelectedMarkerBrush;

    /// <summary>Label foreground.</summary>
    public IBrush LabelBrush { get; set; } = DefaultLabelBrush;

    /// <summary>Circle outline pen.</summary>
    public IPen CirclePen { get; set; } = DefaultCirclePen;

    /// <summary>Circle fill brush.</summary>
    public IBrush CircleFill { get; set; } = DefaultCircleFill;

    /// <summary>Selected coordinate outline pen.</summary>
    public IPen SelectedPen { get; set; } = DefaultSelectedPen;

    /// <summary>Attribution background brush.</summary>
    public IBrush AttributionBackground { get; set; } = DefaultAttributionBackground;

    /// <summary>Pen used for geographic tracks.</summary>
    public IPen TrackPen { get; set; } = DefaultTrackPen;

    /// <summary>Viewport state.</summary>
    public MapViewport Viewport
    {
        get => GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    /// <summary>Markers rendered over the map.</summary>
    public IReadOnlyList<MapMarker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    /// <summary>Circles rendered over the map.</summary>
    public IReadOnlyList<MapCircleOverlay>? Circles
    {
        get => GetValue(CirclesProperty);
        set => SetValue(CirclesProperty, value);
    }

    /// <summary>Connected geographic tracks.</summary>
    public IReadOnlyList<MapTrackOverlay>? Tracks
    {
        get => GetValue(TracksProperty);
        set => SetValue(TracksProperty, value);
    }

    /// <summary>Selected geographic coordinate.</summary>
    public GeoCoordinate? SelectedCoordinate
    {
        get => GetValue(SelectedCoordinateProperty);
        set => SetValue(SelectedCoordinateProperty, value);
    }

    /// <summary>Provider attribution.</summary>
    public string? Attribution
    {
        get => GetValue(AttributionProperty);
        set => SetValue(AttributionProperty, value);
    }

    /// <summary>Optional tile source.</summary>
    public IMapTileSource? TileSource
    {
        get => GetValue(TileSourceProperty);
        set => SetValue(TileSourceProperty, value);
    }

    /// <summary>Whether visible tiles are loading.</summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        private set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>Recoverable map provider error.</summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>Raised when the user selects a geographic coordinate.</summary>
    public event Action<GeoCoordinate>? PointSelected;

    /// <summary>Raised when the user selects a marker.</summary>
    public event Action<MapMarker>? MarkerSelected;

    /// <summary>Sets the viewport center and zoom.</summary>
    public void SetViewport(GeoCoordinate center, double zoom) =>
        Viewport = new MapViewport(center, zoom);

    /// <summary>Zooms around the center by one accessible step.</summary>
    public void ZoomIn() => ZoomAt(
        new Point(Bounds.Width / 2, Bounds.Height / 2),
        Viewport.Zoom + 1);

    /// <summary>Zooms around the center by one accessible step.</summary>
    public void ZoomOut() => ZoomAt(
        new Point(Bounds.Width / 2, Bounds.Height / 2),
        Viewport.Zoom - 1);

    /// <summary>Fits the supplied coordinates with a small viewport margin.</summary>
    public void FitToContent(
        IEnumerable<GeoCoordinate> coordinates,
        double paddingPixels = 48)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        var points = coordinates.ToArray();
        if (points.Length == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var minLatitude = points.Min(point => point.Latitude);
        var maxLatitude = points.Max(point => point.Latitude);
        var minLongitude = points.Min(point => point.Longitude);
        var maxLongitude = points.Max(point => point.Longitude);
        var center = new GeoCoordinate(
            (minLatitude + maxLatitude) / 2,
            (minLongitude + maxLongitude) / 2);
        var longitudeSpan = global::System.Math.Max(0.00001, maxLongitude - minLongitude);
        var latitudeSpan = global::System.Math.Max(0.00001, maxLatitude - minLatitude);
        var usableWidth = global::System.Math.Max(64, Bounds.Width - paddingPixels * 2);
        var usableHeight = global::System.Math.Max(64, Bounds.Height - paddingPixels * 2);
        var zoomByLongitude = global::System.Math.Log(
            usableWidth * 360 / (MapViewportTransform.TileSizePixels * longitudeSpan),
            2);
        var zoomByLatitude = global::System.Math.Log(
            usableHeight * 170 / (MapViewportTransform.TileSizePixels * latitudeSpan),
            2);
        SetViewport(
            center,
            global::System.Math.Clamp(
                global::System.Math.Min(zoomByLongitude, zoomByLatitude),
                MapViewport.MinimumZoom,
                MapViewport.MaximumZoom));
    }

    /// <summary>Returns the current screen/geographic transform.</summary>
    public MapViewportTransform CreateTransform() =>
        new(
            Viewport,
            global::System.Math.Max(1, Bounds.Width),
            global::System.Math.Max(1, Bounds.Height));

    /// <summary>Returns the tile keys visible in the current viewport.</summary>
    public IReadOnlyList<MapTileKey> GetVisibleTileKeys() =>
        CreateTransform().GetVisibleTileKeys();

    /// <summary>Replaces decoded tiles held by the control.</summary>
    public void SetTiles(IEnumerable<MapTile> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        _tiles.Clear();
        foreach (var tile in tiles)
            _tiles[tile.Key] = tile;
        InvalidateVisual();
    }

    /// <summary>Removes all decoded tiles.</summary>
    public void ClearTiles()
    {
        _tiles.Clear();
        InvalidateVisual();
    }

    /// <summary>Loads currently visible tiles from the configured provider.</summary>
    public async Task RefreshTilesAsync(CancellationToken cancellationToken = default)
    {
        var source = TileSource;
        if (source is null)
            return;

        IsLoading = true;
        ErrorMessage = null;
        using var refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previousCancellation = Interlocked.Exchange(
            ref _tileRefreshCancellation,
            refreshCancellation);
        previousCancellation?.Cancel();

        var visibleKeys = GetVisibleTileKeys();
        var pending = visibleKeys
            .Where(key => !_tiles.ContainsKey(key))
            .Select(key => LoadTileAsync(source, key, refreshCancellation.Token))
            .ToList();
        var requested = pending.Count;

        try
        {
            while (pending.Count > 0)
            {
                refreshCancellation.Token.ThrowIfCancellationRequested();
                var completed = await Task.WhenAny(pending);
                pending.Remove(completed);
                var tile = await completed;
                if (tile is null)
                    continue;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _tiles[tile.Key] = tile;
                    InvalidateVisual();
                });
            }

            if (requested > 0
                && visibleKeys.All(key => !_tiles.ContainsKey(key)))
                ErrorMessage = "Map tiles are unavailable. Check the connection and retry.";
        }
        finally
        {
            if (ReferenceEquals(_tileRefreshCancellation, refreshCancellation))
                _tileRefreshCancellation = null;
            previousCancellation?.Dispose();
            IsLoading = false;
        }
    }

    async Task<MapTile?> LoadTileAsync(
        IMapTileSource source,
        MapTileKey key,
        CancellationToken cancellationToken)
    {
        try
        {
            return await source.GetTileAsync(key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewportProperty || change.Property == TileSourceProperty)
            QueueTileRefresh();
    }

    void QueueTileRefresh()
    {
        if (!_isAttached || _tileRefreshQueued)
            return;

        _tileRefreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _tileRefreshQueued = false;
            _ = RefreshTilesSafelyAsync();
        }, DispatcherPriority.Background);
    }

    async Task RefreshTilesSafelyAsync()
    {
        try
        {
            await RefreshTilesAsync();
        }
        catch (OperationCanceledException)
        {
            // A newer viewport or size invalidated this request.
        }
    }

    /// <summary>Zooms around a screen point while retaining the geographic anchor.</summary>
    public void ZoomAt(Point screen, double zoom)
    {
        var transform = CreateTransform();
        var clampedZoom = global::System.Math.Clamp(
            zoom,
            MapViewport.MinimumZoom,
            MapViewport.MaximumZoom);
        var anchor = transform.ScreenToGeo(screen);
        var next = new MapViewport(Viewport.Center, clampedZoom);
        var center = MapViewportTransform.CenterForAnchor(
            next,
            transform.Width,
            transform.Height,
            anchor,
            screen);
        Viewport = new MapViewport(center, clampedZoom);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        Focus();
        _pointerDown = e.GetPosition(this);
        _lastPointer = _pointerDown;
        _isPanning = false;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_lastPointer is not { } last
            || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var current = e.GetPosition(this);
        var delta = current - last;
        if (!_isPanning
            && _pointerDown is { } down
            && ((current.X - down.X) * (current.X - down.X)
                + (current.Y - down.Y) * (current.Y - down.Y)) > 16)
            _isPanning = true;

        if (_isPanning)
        {
            var transform = CreateTransform();
            var center = transform.ScreenToGeo(new Point(
                transform.Width / 2 - delta.X,
                transform.Height / 2 - delta.Y));
            Viewport = new MapViewport(center, Viewport.Zoom);
        }

        _lastPointer = current;
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_pointerDown is null)
            return;

        var point = e.GetPosition(this);
        e.Pointer.Capture(null);

        if (!_isPanning)
        {
            var transform = CreateTransform();
            var marker = HitTestMarker(point, transform);
            if (marker is not null)
            {
                SelectedCoordinate = marker.Position;
                MarkerSelected?.Invoke(marker);
                PointSelected?.Invoke(marker.Position);
            }
            else
            {
                var selected = transform.ScreenToGeo(point);
                SelectedCoordinate = selected;
                PointSelected?.Invoke(selected);
            }
        }

        _pointerDown = null;
        _lastPointer = null;
        _isPanning = false;
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var step = e.Delta.Y > 0 ? 0.5 : -0.5;
        ZoomAt(e.GetPosition(this), Viewport.Zoom + step);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Add:
            case Key.OemPlus:
                ZoomIn();
                e.Handled = true;
                return;
            case Key.Subtract:
            case Key.OemMinus:
                ZoomOut();
                e.Handled = true;
                return;
        }

        var delta = e.Key switch
        {
            Key.Left => new Vector(80, 0),
            Key.Right => new Vector(-80, 0),
            Key.Up => new Vector(0, 80),
            Key.Down => new Vector(0, -80),
            _ => default,
        };
        if (delta != default)
        {
            var transform = CreateTransform();
            var center = transform.ScreenToGeo(new Point(
                transform.Width / 2 + delta.X,
                transform.Height / 2 + delta.Y));
            Viewport = new MapViewport(center, Viewport.Zoom);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(BackgroundBrush, bounds);
        var transform = CreateTransform();

        DrawGrid(context, transform);
        DrawTiles(context, transform);
        DrawCircles(context, transform);
        DrawTracks(context, transform);
        DrawMarkers(context, transform);
        DrawSelectedCoordinate(context, transform);
        DrawStatus(context);
        DrawAttribution(context);
    }

    void DrawGrid(DrawingContext context, MapViewportTransform transform)
    {
        var spacing = global::System.Math.Max(
            32,
            transform.Width / global::System.Math.Pow(2, Viewport.Zoom));
        for (var x = 0d; x < transform.Width; x += spacing)
            context.DrawLine(new Pen(GridBrush, 1), new Point(x, 0), new Point(x, transform.Height));
        for (var y = 0d; y < transform.Height; y += spacing)
            context.DrawLine(new Pen(GridBrush, 1), new Point(0, y), new Point(transform.Width, y));
    }

    void DrawTiles(DrawingContext context, MapViewportTransform transform)
    {
        foreach (var tile in _tiles.Values)
        {
            var destination = transform.TileToScreenRect(tile.Key);
            if (!destination.Intersects(new Rect(0, 0, transform.Width, transform.Height)))
                continue;

            var source = new Rect(
                0,
                0,
                tile.Image.PixelSize.Width,
                tile.Image.PixelSize.Height);
            context.DrawImage(tile.Image, source, destination);
        }
    }

    void DrawCircles(DrawingContext context, MapViewportTransform transform)
    {
        if (Circles is not { Count: > 0 })
            return;

        foreach (var overlay in Circles)
        {
            var center = transform.GeoToScreen(overlay.Circle.Center);
            var latitudeRadians = overlay.Circle.Center.Latitude * global::System.Math.PI / 180d;
            var metersPerPixel = 2 * global::System.Math.PI
                * GeoDistance.MeanEarthRadiusMeters
                * global::System.Math.Max(0.01, global::System.Math.Cos(latitudeRadians))
                / transform.WorldPixels;
            var radiusPixels = overlay.Circle.RadiusMeters / metersPerPixel;
            context.DrawEllipse(CircleFill, CirclePen, center, radiusPixels, radiusPixels);
        }
    }

    void DrawTracks(DrawingContext context, MapViewportTransform transform)
    {
        if (Tracks is not { Count: > 0 })
            return;

        foreach (var track in Tracks)
        {
            if (track.Points.Count < 2)
                continue;

            for (var index = 1; index < track.Points.Count; index++)
                context.DrawLine(
                    TrackPen,
                    transform.GeoToScreen(track.Points[index - 1]),
                    transform.GeoToScreen(track.Points[index]));
        }
    }

    void DrawMarkers(DrawingContext context, MapViewportTransform transform)
    {
        if (Markers is not { Count: > 0 })
            return;

        var typeface = new Typeface("Segoe UI,sans-serif");
        foreach (var marker in Markers)
        {
            var screen = transform.GeoToScreen(marker.Position);
            var radius = double.IsFinite(marker.RadiusPixels)
                ? global::System.Math.Max(2, marker.RadiusPixels)
                : 6;
            var selected = SelectedCoordinate == marker.Position;
            context.DrawEllipse(
                selected ? SelectedMarkerBrush : MarkerBrush,
                selected ? SelectedPen : null,
                screen,
                radius,
                radius);

            if (string.IsNullOrWhiteSpace(marker.Label))
                continue;

            var formatted = new FormattedText(
                marker.Label,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                LabelBrush);
            context.DrawText(formatted, new Point(screen.X + radius + 4, screen.Y - formatted.Height / 2));
        }
    }

    void DrawSelectedCoordinate(DrawingContext context, MapViewportTransform transform)
    {
        if (SelectedCoordinate is not { } selected
            || Markers?.Any(marker => marker.Position == selected) == true)
            return;

        var screen = transform.GeoToScreen(selected);
        context.DrawEllipse(null, SelectedPen, screen, 8, 8);
        context.DrawLine(
            SelectedPen,
            new Point(screen.X - 11, screen.Y),
            new Point(screen.X + 11, screen.Y));
        context.DrawLine(
            SelectedPen,
            new Point(screen.X, screen.Y - 11),
            new Point(screen.X, screen.Y + 11));
    }

    void DrawAttribution(DrawingContext context)
    {
        if (string.IsNullOrWhiteSpace(Attribution))
            return;

        var typeface = new Typeface("Segoe UI,sans-serif");
        var formatted = new FormattedText(
            Attribution,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            10,
            LabelBrush);
        var padding = 4;
        var rect = new Rect(
            0,
            Bounds.Height - formatted.Height - padding * 2,
            formatted.Width + padding * 2,
            formatted.Height + padding * 2);
        context.FillRectangle(AttributionBackground, rect);
        context.DrawText(formatted, new Point(padding, Bounds.Height - formatted.Height - padding));
    }

    void DrawStatus(DrawingContext context)
    {
        if (!IsLoading && string.IsNullOrWhiteSpace(ErrorMessage))
            return;

        var text = IsLoading ? "Loading map…" : ErrorMessage!;
        var typeface = new Typeface("Segoe UI,sans-serif");
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            12,
            LabelBrush);
        var padding = 10;
        var rect = new Rect(
            10,
            10,
            formatted.Width + padding * 2,
            formatted.Height + padding * 2);
        context.FillRectangle(AttributionBackground, rect);
        context.DrawText(formatted, new Point(10 + padding, 10 + padding));
    }

    MapMarker? HitTestMarker(Point point, MapViewportTransform transform)
    {
        if (Markers is not { Count: > 0 })
            return null;

        for (var index = Markers.Count - 1; index >= 0; index--)
        {
            var marker = Markers[index];
            var screen = transform.GeoToScreen(marker.Position);
            var radius = double.IsFinite(marker.RadiusPixels)
                ? global::System.Math.Max(2, marker.RadiusPixels)
                : 6;
            var hitRadius = radius + 8;
            var deltaX = screen.X - point.X;
            var deltaY = screen.Y - point.Y;
            if (deltaX * deltaX + deltaY * deltaY <= hitRadius * hitRadius)
                return marker;
        }

        return null;
    }
}
