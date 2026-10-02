using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Map;

/// <summary>
/// Provider-neutral geographic map control with pan, zoom, selection, markers, circles, tiles, and attribution.
/// </summary>
public sealed class MapControl : Control
{
    const int MaximumCachedTiles = 128;

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
    readonly Dictionary<MapTileKey, long> _tileLastUsed = new();
    long _tileUseCounter;
    CancellationTokenSource? _tileRefreshCancellation;
    long _tileRefreshGeneration;
    bool _isAttached;
    bool _tileRefreshQueued;
    readonly Dictionary<int, Point> _contacts = new();
    readonly Dictionary<int, IPointer> _pointers = new();
    readonly List<GeoCoordinate> _drawingPoints = [];
    int? _panPointerId;
    int? _drawingPointerId;
    Point _panOrigin;
    bool _panFromTouch;
    bool _isPanning;
    bool _isPinching;
    long _lastMoveTicks;
    Vector _panVelocity;
    double _pinchStartDistance;
    MapViewport _pinchStartViewport;
    GeoCoordinate _pinchAnchor;
    bool _suspendTileRefresh;
    bool _tilesStale;
    GeoDrawingKind? _drawingKind;
    DispatcherTimer? _inertiaTimer;
    Vector _inertiaVelocity;
    long _lastTapTicks;
    Point _lastTapPoint;
    bool _suppressSelectionChanged;

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

    /// <summary>Polygon overlays rendered over the map.</summary>
    public static readonly StyledProperty<IReadOnlyList<MapPolygonOverlay>?> PolygonsProperty =
        AvaloniaProperty.Register<MapControl, IReadOnlyList<MapPolygonOverlay>?>(nameof(Polygons));

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

    /// <summary>Whether one or more visible tiles came from a stale cache fallback.</summary>
    public static readonly StyledProperty<bool> HasStaleTilesProperty =
        AvaloniaProperty.Register<MapControl, bool>(nameof(HasStaleTiles));

    static MapControl()
    {
        AffectsRender<MapControl>(
            ViewportProperty,
            MarkersProperty,
            CirclesProperty,
            TracksProperty,
            PolygonsProperty,
            SelectedCoordinateProperty,
            AttributionProperty,
            TileSourceProperty,
            IsLoadingProperty,
            ErrorMessageProperty,
            HasStaleTilesProperty);
    }

    /// <summary>Creates a map control.</summary>
    public MapControl()
    {
        ClipToBounds = true;
        Focusable = true;
        SetValue(AutomationProperties.NameProperty, "Map");
        AttachedToVisualTree += (_, _) =>
        {
            _isAttached = true;
            QueueTileRefresh();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _isAttached = false;
            StopInertia();
            ClearTiles();
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

    /// <summary>Polygon overlays rendered over the map.</summary>
    public IReadOnlyList<MapPolygonOverlay>? Polygons
    {
        get => GetValue(PolygonsProperty);
        set => SetValue(PolygonsProperty, value);
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

    /// <summary>Whether a visible tile is being shown from an older cache entry.</summary>
    public bool HasStaleTiles
    {
        get => GetValue(HasStaleTilesProperty);
        private set => SetValue(HasStaleTilesProperty, value);
    }

    /// <summary>Raised when the user selects a geographic coordinate.</summary>
    public event Action<GeoCoordinate>? PointSelected;

    /// <summary>Raised when the user selects a marker.</summary>
    public event Action<MapMarker>? MarkerSelected;

    /// <summary>Raised when a drawing session is completed.</summary>
    public event Action<GeoDrawing>? DrawingCompleted;

    /// <summary>Raised when the selected marker or coordinate changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>Optional capabilities enabled by the host.</summary>
    public MapInteractionOptions InteractionOptions { get; set; } =
        MapInteractionOptions.Disabled;

    /// <summary>Optional host-provided clipboard writer used by tests or product policy.</summary>
    public Func<string, CancellationToken, Task>? ClipboardWriter { get; set; }

    /// <summary>Currently selected marker, if the selected coordinate came from one.</summary>
    public MapMarker? SelectedMarker { get; private set; }

    /// <summary>Selects a coordinate as a user-facing map interaction.</summary>
    public void SelectCoordinate(GeoCoordinate coordinate)
    {
        var marker = Markers?.FirstOrDefault(item => item.Position == coordinate);
        ApplyInteractionSelection(coordinate, marker);
    }

    /// <summary>Selects a marker and retains its metadata and host tag.</summary>
    public void SelectMarker(MapMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);
        ApplyInteractionSelection(marker.Position, marker);
    }

    /// <summary>Whether a host-started geographic drawing session is active.</summary>
    public bool IsDrawing => _drawingKind is not null;

    /// <summary>Vertices collected by the active drawing session.</summary>
    public IReadOnlyList<GeoCoordinate> DrawingPoints => _drawingPoints;

    /// <summary>Sets the viewport center and zoom.</summary>
    public void SetViewport(GeoCoordinate center, double zoom) =>
        Viewport = new MapViewport(center, zoom);

    /// <summary>Copies the selected coordinate using the configured host clipboard.</summary>
    public Task<bool> CopySelectedCoordinateAsync(
        CancellationToken cancellationToken = default) =>
        CopySelectionAsync(
            SelectedMarker is { } marker
                ? GeoCoordinateText.Format(marker.Position)
                : SelectedCoordinate is { } coordinate
                    ? GeoCoordinateText.Format(coordinate)
                    : null,
            cancellationToken);

    /// <summary>Copies the selected coordinate and marker identity as JSON.</summary>
    public Task<bool> CopySelectedJsonAsync(
        CancellationToken cancellationToken = default) =>
        CopySelectionAsync(
            SelectedCoordinate is not { } coordinate
                ? null
                : GeoCoordinateText.ToJson(
                    coordinate,
                    SelectedMarker?.Id,
                    SelectedMarker?.Label,
                    SelectedMarker?.Metadata),
            cancellationToken);

    /// <summary>Executes a host-mapped keyboard command.</summary>
    public async Task<bool> ExecuteKeyboardCommandAsync(
        MapKeyboardCommand command,
        CancellationToken cancellationToken = default)
    {
        switch (command)
        {
            case MapKeyboardCommand.CopyCoordinate:
                if (!InteractionOptions.EnableClipboardShortcuts)
                    return false;
                return await CopySelectedCoordinateAsync(cancellationToken);
            case MapKeyboardCommand.CopyJson:
                if (!InteractionOptions.EnableClipboardShortcuts)
                    return false;
                return await CopySelectedJsonAsync(cancellationToken);
            case MapKeyboardCommand.ZoomIn:
                if (!InteractionOptions.EnableKeyboardNavigation)
                    return false;
                ZoomIn();
                return true;
            case MapKeyboardCommand.ZoomOut:
                if (!InteractionOptions.EnableKeyboardNavigation)
                    return false;
                ZoomOut();
                return true;
            case MapKeyboardCommand.PanLeft:
                if (!InteractionOptions.EnableKeyboardNavigation)
                    return false;
                Viewport = CreateTransform().Translate(new Vector(80, 0));
                return true;
            case MapKeyboardCommand.PanRight:
                if (!InteractionOptions.EnableKeyboardNavigation)
                    return false;
                Viewport = CreateTransform().Translate(new Vector(-80, 0));
                return true;
            case MapKeyboardCommand.PanUp:
                if (!InteractionOptions.EnableKeyboardNavigation)
                    return false;
                Viewport = CreateTransform().Translate(new Vector(0, 80));
                return true;
            case MapKeyboardCommand.PanDown:
                if (!InteractionOptions.EnableKeyboardNavigation)
                    return false;
                Viewport = CreateTransform().Translate(new Vector(0, -80));
                return true;
            case MapKeyboardCommand.CompleteDrawing:
                if (!InteractionOptions.EnableDrawing)
                    return false;
                return CompleteDrawing() is not null;
            case MapKeyboardCommand.CancelDrawing:
                if (!InteractionOptions.EnableDrawing || !IsDrawing)
                    return false;
                CancelDrawing();
                return true;
            default:
                return false;
        }
    }

    /// <summary>Starts a host-enabled geographic drawing session.</summary>
    public bool BeginDrawing(GeoDrawingKind kind)
    {
        if (!InteractionOptions.EnableDrawing)
            return false;

        _drawingKind = kind;
        _drawingPoints.Clear();
        InvalidateVisual();
        return true;
    }

    /// <summary>Adds a vertex to the active drawing session.</summary>
    public bool AddDrawingPoint(GeoCoordinate coordinate)
    {
        if (_drawingKind is not { } kind)
            return false;

        if (kind == GeoDrawingKind.Circle && _drawingPoints.Count >= 1)
        {
            if (_drawingPoints.Count == 1)
                _drawingPoints.Add(coordinate);
            else
                _drawingPoints[1] = coordinate;
        }
        else
        {
            _drawingPoints.Add(coordinate);
        }
        InvalidateVisual();
        if (kind == GeoDrawingKind.Point)
            CompleteDrawing();
        return true;
    }

    /// <summary>Completes the active drawing and raises <see cref="DrawingCompleted"/>.</summary>
    public GeoDrawing? CompleteDrawing()
    {
        if (_drawingKind is not { } kind
            || !HasEnoughDrawingPoints(kind, _drawingPoints.Count))
        {
            return null;
        }

        var drawing = new GeoDrawing(kind, _drawingPoints);
        _drawingKind = null;
        _drawingPoints.Clear();
        InvalidateVisual();
        DrawingCompleted?.Invoke(drawing);
        return drawing;
    }

    /// <summary>Cancels the active drawing without raising completion.</summary>
    public void CancelDrawing()
    {
        if (_drawingKind is null)
            return;

        _drawingKind = null;
        _drawingPoints.Clear();
        InvalidateVisual();
    }

    /// <summary>Requests an immediate tile refresh after a provider or network failure.</summary>
    public void RetryTiles()
    {
        ErrorMessage = null;
        QueueTileRefresh(immediate: true);
    }

    /// <summary>Requests a redraw after a host mutates an overlay collection in place.</summary>
    public void RequestRender() => InvalidateVisual();

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
        var (centerLongitude, longitudeSpan) = LongitudeFrame(points);
        var center = new GeoCoordinate(
            (minLatitude + maxLatitude) / 2,
            centerLongitude);
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

    static (double Center, double Span) LongitudeFrame(IReadOnlyList<GeoCoordinate> points)
    {
        var longitudes = points
            .Select(point => point.Longitude < 0 ? point.Longitude + 360 : point.Longitude)
            .OrderBy(longitude => longitude)
            .ToArray();
        if (longitudes.Length == 1)
            return (NormalizeLongitude(longitudes[0]), 0.00001);

        var largestGap = -1d;
        var largestGapIndex = 0;
        for (var index = 0; index < longitudes.Length; index++)
        {
            var next = index + 1 < longitudes.Length
                ? longitudes[index + 1]
                : longitudes[0] + 360;
            var gap = next - longitudes[index];
            if (gap > largestGap)
            {
                largestGap = gap;
                largestGapIndex = index;
            }
        }

        var start = longitudes[(largestGapIndex + 1) % longitudes.Length];
        var span = global::System.Math.Clamp(360 - largestGap, 0.00001, 360);
        return (NormalizeLongitude(start + span / 2), span);
    }

    static double NormalizeLongitude(double longitude)
    {
        var normalized = longitude % 360;
        if (normalized > 180)
            normalized -= 360;
        if (normalized < -180)
            normalized += 360;
        return normalized;
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
        DisposeTiles();
        _tiles.Clear();
        _tileLastUsed.Clear();
        foreach (var tile in tiles)
        {
            _tiles[tile.Key] = tile;
            _tileLastUsed[tile.Key] = ++_tileUseCounter;
        }
        TrimTiles(GetVisibleTileKeys());
        HasStaleTiles = _tiles.Values.Any(tile => tile.IsStale);
        InvalidateVisual();
    }

    /// <summary>Removes all decoded tiles.</summary>
    public void ClearTiles()
    {
        Interlocked.Increment(ref _tileRefreshGeneration);
        _tileRefreshCancellation?.Cancel();
        DisposeTiles();
        _tiles.Clear();
        _tileLastUsed.Clear();
        _tilesStale = false;
        HasStaleTiles = false;
        InvalidateVisual();
    }

    void DisposeTiles()
    {
        foreach (var tile in _tiles.Values)
        {
            if (tile.Image is IDisposable disposable)
                disposable.Dispose();
        }
    }

    static void DisposeTile(MapTile? tile)
    {
        if (tile?.Image is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>Loads currently visible tiles from the configured provider.</summary>
    public async Task RefreshTilesAsync(CancellationToken cancellationToken = default)
    {
        var source = TileSource;
        if (source is null)
            return;

        var generation = Interlocked.Increment(ref _tileRefreshGeneration);
        IsLoading = true;
        ErrorMessage = null;
        using var refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previousCancellation = Interlocked.Exchange(
            ref _tileRefreshCancellation,
            refreshCancellation);
        previousCancellation?.Cancel();

        var visibleKeys = GetVisibleTileKeys();
        var pending = visibleKeys
            .Where(key => !_tiles.TryGetValue(key, out var tile) || tile.IsStale)
            .Select(key => LoadTileAsync(source, key, refreshCancellation.Token))
            .ToList();
        var failed = 0;

        try
        {
            while (pending.Count > 0)
            {
                refreshCancellation.Token.ThrowIfCancellationRequested();
                var completed = await Task.WhenAny(pending);
                pending.Remove(completed);
                var result = await completed;
                if (refreshCancellation.Token.IsCancellationRequested
                    || !IsCurrentTileRefresh(source, generation))
                {
                    DisposeTile(result.Tile);
                    return;
                }

                if (result.Tile is null)
                {
                    if (result.Failed)
                        failed++;
                    continue;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (refreshCancellation.Token.IsCancellationRequested
                        || !IsCurrentTileRefresh(source, generation))
                    {
                        DisposeTile(result.Tile);
                        return;
                    }

                    ReplaceTile(result.Tile);
                    _tileLastUsed[result.Tile.Key] = ++_tileUseCounter;
                    InvalidateVisual();
                });
            }

            if (refreshCancellation.Token.IsCancellationRequested
                || !IsCurrentTileRefresh(source, generation))
                return;

            TrimTiles(visibleKeys);
            HasStaleTiles = visibleKeys.Any(
                key => _tiles.TryGetValue(key, out var tile) && tile.IsStale);
            if (failed > 0)
            {
                var loaded = visibleKeys.Count(key => _tiles.ContainsKey(key));
                ErrorMessage = loaded > 0
                    ? $"{failed} map tile{(failed == 1 ? string.Empty : "s")} unavailable. Retry."
                    : "Map tiles are unavailable. Check the connection and retry.";
            }
        }
        catch (OperationCanceledException) when (
            refreshCancellation.IsCancellationRequested
            || !IsCurrentTileRefresh(source, generation))
        {
            // A newer viewport, source, or host lifecycle superseded this refresh.
        }
        catch (Exception exception) when (IsCurrentTileRefresh(source, generation))
        {
            ErrorMessage = $"Map tiles are unavailable: {exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(_tileRefreshCancellation, refreshCancellation))
            {
                _tileRefreshCancellation = null;
                IsLoading = false;
                InvalidateVisual();
            }
        }
    }

    readonly record struct TileLoadResult(MapTile? Tile, bool Failed);

    bool IsCurrentTileRefresh(IMapTileSource source, long generation) =>
        ReferenceEquals(source, TileSource)
        && Volatile.Read(ref _tileRefreshGeneration) == generation;

    void ReplaceTile(MapTile tile)
    {
        if (_tiles.TryGetValue(tile.Key, out var previous)
            && !ReferenceEquals(previous.Image, tile.Image)
            && previous.Image is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _tiles[tile.Key] = tile;
        _tileLastUsed[tile.Key] = ++_tileUseCounter;
        HasStaleTiles = _tiles.Values.Any(item => item.IsStale);
    }

    void TrimTiles(IReadOnlyCollection<MapTileKey> visibleKeys)
    {
        var visible = visibleKeys.ToHashSet();
        while (_tiles.Count > MaximumCachedTiles)
        {
            var victim = _tileLastUsed
                .Where(item => !visible.Contains(item.Key))
                .OrderBy(item => item.Value)
                .Select(item => item.Key)
                .FirstOrDefault();
            if (!_tiles.ContainsKey(victim))
            {
                victim = _tileLastUsed
                    .OrderBy(item => item.Value)
                    .Select(item => item.Key)
                    .FirstOrDefault();
            }

            if (!_tiles.ContainsKey(victim))
                break;

            if (_tiles.Remove(victim, out var tile))
            {
                if (tile.Image is IDisposable disposable)
                    disposable.Dispose();
                _tileLastUsed.Remove(victim);
            }
        }
    }

    async Task<TileLoadResult> LoadTileAsync(
        IMapTileSource source,
        MapTileKey key,
        CancellationToken cancellationToken)
    {
        try
        {
            var tile = await source.GetTileAsync(key, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                DisposeTile(tile);
                return new TileLoadResult(null, Failed: false);
            }

            return new TileLoadResult(tile, Failed: tile is null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new TileLoadResult(null, Failed: false);
        }
        catch
        {
            return new TileLoadResult(null, Failed: true);
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TileSourceProperty)
        {
            ClearTiles();
            QueueTileRefresh();
            return;
        }

        if (change.Property == SelectedCoordinateProperty)
        {
            var oldCoordinate = change.GetOldValue<GeoCoordinate?>();
            SelectedMarker = Markers?.FirstOrDefault(
                marker => marker.Position == SelectedCoordinate);
            if (oldCoordinate != SelectedCoordinate
                && !_suppressSelectionChanged)
            {
                SelectionChanged?.Invoke();
            }
            return;
        }

        if (change.Property == MarkersProperty)
        {
            SelectedMarker = Markers?.FirstOrDefault(
                marker => marker.Position == SelectedCoordinate);
            return;
        }

        if (change.Property != ViewportProperty)
            return;

        if (_suspendTileRefresh)
        {
            _tilesStale = true;
            return;
        }

        QueueTileRefresh();
    }

    void QueueTileRefresh(bool immediate = false)
    {
        if (!_isAttached || _tileRefreshQueued)
            return;

        _tileRefreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _tileRefreshQueued = false;
            _ = RefreshTilesSafelyAsync();
        }, immediate ? DispatcherPriority.Input : DispatcherPriority.Background);
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
        if (!IsMapContact(e))
            return;

        if (InteractionOptions.EnableDrawing
            && IsDrawing)
        {
            var drawingPosition = e.GetPosition(this);
            var coordinate = CreateTransform().ScreenToGeo(drawingPosition);
            Focus();
            e.PreventGestureRecognition();
            e.Handled = true;
            if (_drawingKind == GeoDrawingKind.Circle)
            {
                _drawingPointerId = e.Pointer.Id;
                e.Pointer.Capture(this);
                AddDrawingPoint(coordinate);
            }
            else
            {
                AddDrawingPoint(coordinate);
            }

            return;
        }

        StopInertia();
        var position = e.GetPosition(this);
        _contacts[e.Pointer.Id] = position;
        _pointers[e.Pointer.Id] = e.Pointer;
        var touch = IsTouch(e);

        if (touch && _contacts.Count >= 2)
        {
            OwnTwoFingerGesture(e);
            BeginPinch();
            return;
        }

        if (touch)
        {
            e.PreventGestureRecognition();
            e.Pointer.Capture(this);
            e.Handled = true;
            _panPointerId = e.Pointer.Id;
            _panOrigin = position;
            _panFromTouch = true;
            _isPanning = false;
            _panVelocity = default;
            _lastMoveTicks = Environment.TickCount64;
            return;
        }

        Focus();
        e.PreventGestureRecognition();
        e.Pointer.Capture(this);
        e.Handled = true;
        _panPointerId = e.Pointer.Id;
        _panOrigin = position;
        _panFromTouch = false;
        _isPanning = false;
        _panVelocity = default;
        _lastMoveTicks = Environment.TickCount64;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_drawingPointerId == e.Pointer.Id
            && _drawingKind == GeoDrawingKind.Circle
            && _drawingPoints.Count > 0)
        {
            var coordinate = CreateTransform().ScreenToGeo(e.GetPosition(this));
            if (_drawingPoints.Count == 1)
                _drawingPoints.Add(coordinate);
            else
                _drawingPoints[1] = coordinate;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (!_contacts.ContainsKey(e.Pointer.Id))
            return;

        var current = e.GetPosition(this);
        var previous = _contacts[e.Pointer.Id];
        _contacts[e.Pointer.Id] = current;

        if (_contacts.Count >= 2)
        {
            if (!_isPinching)
                BeginPinch();
            OwnTwoFingerGesture(e);
            ApplyPinch();
            return;
        }

        if (_panPointerId != e.Pointer.Id)
            return;

        var delta = current - previous;
        if (!_isPanning)
        {
            var movedX = current.X - _panOrigin.X;
            var movedY = current.Y - _panOrigin.Y;
            if (movedX * movedX + movedY * movedY < 36)
            {
                e.Handled = true;
                return;
            }

            _isPanning = true;
            _suspendTileRefresh = true;
        }

        var now = Environment.TickCount64;
        var elapsed = now - _lastMoveTicks;
        if (elapsed is > 0 and < 80)
            _panVelocity = delta / (elapsed / 1000d);
        else
            _panVelocity = default;

        _lastMoveTicks = now;
        TranslateBy(delta);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_drawingPointerId == e.Pointer.Id)
        {
            _drawingPointerId = null;
            var coordinate = CreateTransform().ScreenToGeo(e.GetPosition(this));
            if (_drawingKind == GeoDrawingKind.Circle)
            {
                if (_drawingPoints.Count == 1)
                    _drawingPoints.Add(coordinate);
                else
                    _drawingPoints[1] = coordinate;
            }

            e.Pointer.Capture(null);
            CompleteDrawing();
            e.Handled = true;
            return;
        }

        if (!_contacts.Remove(e.Pointer.Id))
            return;

        _pointers.Remove(e.Pointer.Id);
        var point = e.GetPosition(this);
        e.Pointer.Capture(null);

        if (_isPinching)
        {
            e.PreventGestureRecognition();
            e.Handled = true;
            if (_contacts.Count >= 2)
                return;

            _isPinching = false;
            if (_contacts.Count == 1)
            {
                var remaining = _contacts.Keys.First();
                _panPointerId = remaining;
                _panOrigin = _contacts[remaining];
                _panFromTouch = true;
                _isPanning = false;
                _panVelocity = default;
                _lastMoveTicks = Environment.TickCount64;
            }
            FinishGesture();
            return;
        }

        if (_panFromTouch)
        {
            if (_panPointerId != e.Pointer.Id)
                return;

            var movedX = point.X - _panOrigin.X;
            var movedY = point.Y - _panOrigin.Y;
            _panPointerId = null;
            _panFromTouch = false;
            if (_isPanning)
            {
                e.Handled = true;
                BeginInertia(_panVelocity);
                return;
            }

            if (movedX * movedX + movedY * movedY < 576)
                SelectAt(point);
            FinishGesture();
            return;
        }

        e.Handled = true;
        if (_panPointerId != e.Pointer.Id)
            return;

        _panPointerId = null;
        if (_isPanning)
        {
            BeginInertia(_panVelocity);
            return;
        }

        SelectAt(point);
        FinishGesture();
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        if (_drawingPointerId == e.Pointer.Id)
        {
            _drawingPointerId = null;
            CancelDrawing();
            return;
        }

        if (!_contacts.Remove(e.Pointer.Id))
            return;

        _pointers.Remove(e.Pointer.Id);
        if (_panPointerId == e.Pointer.Id)
            _panPointerId = null;

        if (_contacts.Count < 2)
            _isPinching = false;

        if (_contacts.Count == 0)
        {
            StopInertia();
            FinishGesture();
        }
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        if (global::System.Math.Abs(delta) < 0.01)
            return;

        var zoomDelta = global::System.Math.Abs(delta) >= 1
            ? global::System.Math.Sign(delta) * 0.5
            : delta * 0.35;
        ZoomAt(e.GetPosition(this), Viewport.Zoom + zoomDelta);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (InteractionOptions.EnableClipboardShortcuts
            && control
            && e.Key == Key.C)
        {
            _ = shift
                ? CopySelectedJsonAsync()
                : CopySelectedCoordinateAsync();
            e.Handled = true;
            return;
        }

        if (InteractionOptions.EnableDrawing && IsDrawing)
        {
            if (e.Key == Key.Escape)
            {
                CancelDrawing();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                CompleteDrawing();
                e.Handled = true;
                return;
            }
        }

        if (!InteractionOptions.EnableKeyboardNavigation)
            return;

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
        DrawPolygons(context, transform);
        DrawTracks(context, transform);
        DrawMarkers(context, transform);
        DrawSelectedCoordinate(context, transform);
        DrawDrawingPreview(context, transform);
        DrawDrawingStatus(context);
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
        foreach (var key in transform.GetVisibleTileKeys())
        {
            if (!_tiles.TryGetValue(key, out var tile))
                continue;

            _tileLastUsed[key] = ++_tileUseCounter;
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
            var fill = overlay.Ink is { } ink
                ? new SolidColorBrush(Color.FromArgb(48, ink.R, ink.G, ink.B))
                : CircleFill;
            var pen = overlay.Ink is { } stroke
                ? new Pen(new SolidColorBrush(stroke), 2)
                : CirclePen;
            context.DrawEllipse(fill, pen, center, radiusPixels, radiusPixels);
            DrawLabel(context, overlay.Label, center);
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

            var pixels = WebMercatorTiles.GeoPathToPixels(
                Viewport.Center,
                Viewport.Zoom,
                transform.Width,
                transform.Height,
                track.Points);
            var segments = track.Points.Count - 1;
            for (var index = 1; index < track.Points.Count; index++)
            {
                var pen = TrackPen;
                if (track.FromInk is { } from && track.ToInk is { } to)
                {
                    var amount = segments <= 1 ? 1 : (index - 1) / (double)(segments - 1);
                    pen = new Pen(
                        new SolidColorBrush(MapInk.Lerp(from, to, amount)),
                        3,
                        lineCap: PenLineCap.Round);
                }
                else if (track.FromInk is { } solid)
                {
                    pen = new Pen(new SolidColorBrush(solid), 3, lineCap: PenLineCap.Round);
                }

                context.DrawLine(
                    pen,
                    new Point(pixels[index - 1].X, pixels[index - 1].Y),
                    new Point(pixels[index].X, pixels[index].Y));
            }

            var labelPoint = pixels[pixels.Count / 2];
            DrawLabel(context, track.Label, new Point(labelPoint.X, labelPoint.Y));
        }
    }

    void DrawPolygons(DrawingContext context, MapViewportTransform transform)
    {
        if (Polygons is not { Count: > 0 })
            return;

        foreach (var polygon in Polygons)
        {
            if (polygon.Points.Count < 2)
                continue;

            if (polygon.Fill is { } fill && polygon.Points.Count >= 3)
            {
                var points = WebMercatorTiles.GeoPathToPixels(
                        Viewport.Center,
                        Viewport.Zoom,
                        transform.Width,
                        transform.Height,
                        polygon.Points)
                    .Select(point => new Point(point.X, point.Y))
                    .ToList();
                if (points[0] != points[^1])
                    points.Add(points[0]);
                context.DrawGeometry(
                    new SolidColorBrush(fill),
                    null,
                    new PolylineGeometry(points, isFilled: true));
            }

            var pathPixels = WebMercatorTiles.GeoPathToPixels(
                Viewport.Center,
                Viewport.Zoom,
                transform.Width,
                transform.Height,
                polygon.Points);
            var pen = polygon.Ink is { } ink
                ? new Pen(new SolidColorBrush(ink), 2)
                : TrackPen;
            for (var index = 1; index < polygon.Points.Count; index++)
            {
                context.DrawLine(
                    pen,
                    new Point(pathPixels[index - 1].X, pathPixels[index - 1].Y),
                    new Point(pathPixels[index].X, pathPixels[index].Y));
            }

            if (polygon.Points[0] != polygon.Points[^1])
            {
                context.DrawLine(
                    pen,
                    new Point(pathPixels[^1].X, pathPixels[^1].Y),
                    new Point(pathPixels[0].X, pathPixels[0].Y));
            }

            var labelPoint = new Point(
                pathPixels.Average(point => point.X),
                pathPixels.Average(point => point.Y));
            DrawLabel(context, polygon.Label, labelPoint);
        }
    }

    void DrawDrawingStatus(DrawingContext context)
    {
        if (!InteractionOptions.ShowMeasurementResults
            || _drawingKind is not { } kind)
        {
            return;
        }

        var text = GeoMeasurementText.ForDrawing(kind, _drawingPoints);
        var typeface = new Typeface("Segoe UI,sans-serif");
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            12,
            LabelBrush);
        var padding = 10;
        var y = global::System.Math.Max(
            48,
            Bounds.Height - (string.IsNullOrWhiteSpace(Attribution) ? 48 : 78));
        var rect = new Rect(
            10,
            y,
            formatted.Width + padding * 2,
            formatted.Height + padding * 2);
        context.FillRectangle(AttributionBackground, rect);
        context.DrawText(formatted, new Point(10 + padding, y + padding));
    }

    void DrawDrawingPreview(
        DrawingContext context,
        MapViewportTransform transform)
    {
        if (_drawingKind is not { } kind || _drawingPoints.Count == 0)
            return;

        var pen = new Pen(new SolidColorBrush(Color.Parse("#d28b38")), 3);
        if (kind == GeoDrawingKind.Circle && _drawingPoints.Count >= 2)
        {
            var center = transform.GeoToScreen(_drawingPoints[0]);
            var edge = transform.GeoToScreen(_drawingPoints[1]);
            var radius = Distance(center, edge);
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var pixels = WebMercatorTiles.GeoPathToPixels(
            Viewport.Center,
            Viewport.Zoom,
            transform.Width,
            transform.Height,
            _drawingPoints);
        for (var index = 1; index < pixels.Count; index++)
        {
            context.DrawLine(
                pen,
                new Point(pixels[index - 1].X, pixels[index - 1].Y),
                new Point(pixels[index].X, pixels[index].Y));
        }

        if (kind == GeoDrawingKind.Polygon && pixels.Count >= 3)
        {
            context.DrawLine(
                pen,
                new Point(pixels[^1].X, pixels[^1].Y),
                new Point(pixels[0].X, pixels[0].Y));
        }
    }

    void DrawLabel(DrawingContext context, string? label, Point anchor)
    {
        if (string.IsNullOrWhiteSpace(label))
            return;

        var formatted = new FormattedText(
            label,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI,sans-serif"),
            11,
            LabelBrush);
        context.DrawText(
            formatted,
            new Point(anchor.X + 6, anchor.Y - formatted.Height / 2));
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
            var brush = marker.Ink is { } ink
                ? new SolidColorBrush(ink)
                : selected ? SelectedMarkerBrush : MarkerBrush;
            context.DrawEllipse(
                brush,
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
        var attribution = string.IsNullOrWhiteSpace(Attribution)
            ? (TileSource as RasterMapTileSource)?.Attribution
            : Attribution;
        if (string.IsNullOrWhiteSpace(attribution))
            return;

        var typeface = new Typeface("Segoe UI,sans-serif");
        var formatted = new FormattedText(
            attribution,
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
        if (!IsLoading
            && string.IsNullOrWhiteSpace(ErrorMessage)
            && !HasStaleTiles)
            return;

        var text = IsLoading
            ? "Loading map…"
            : ErrorMessage ?? "Using cached map tiles";
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

    static bool IsTouch(PointerEventArgs e) =>
        e.Pointer.Type is PointerType.Touch or PointerType.Pen;

    void OwnTwoFingerGesture(PointerEventArgs e)
    {
        e.PreventGestureRecognition();
        foreach (var pointer in _pointers.Values)
            pointer.Capture(this);
        e.Handled = true;
        Focus();
    }

    bool IsMapContact(PointerEventArgs e)
    {
        if (e.Pointer.Type is PointerType.Touch or PointerType.Pen)
            return true;

        return e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
    }

    void BeginPinch()
    {
        if (!TryPinchPoints(out var first, out var second))
            return;

        _isPinching = true;
        _isPanning = false;
        _suspendTileRefresh = true;
        _pinchStartViewport = Viewport;
        _pinchStartDistance = global::System.Math.Max(1, Distance(first, second));
        _pinchAnchor = CreateTransform().ScreenToGeo(Midpoint(first, second));
    }

    void ApplyPinch()
    {
        if (!TryPinchPoints(out var first, out var second))
            return;

        Viewport = MapViewportTransform.Pinch(
            _pinchStartViewport,
            global::System.Math.Max(1, Bounds.Width),
            global::System.Math.Max(1, Bounds.Height),
            _pinchAnchor,
            Midpoint(first, second),
            _pinchStartDistance,
            Distance(first, second));
    }

    void TranslateBy(Vector screenDelta)
    {
        if (screenDelta.X == 0 && screenDelta.Y == 0)
            return;

        Viewport = CreateTransform().Translate(screenDelta);
    }

    void SelectAt(Point point)
    {
        var now = Environment.TickCount64;
        if (now - _lastTapTicks is > 0 and < 280
            && Distance(point, _lastTapPoint) < 28)
        {
            _lastTapTicks = 0;
            ZoomAt(point, Viewport.Zoom + 1);
            return;
        }

        _lastTapTicks = now;
        _lastTapPoint = point;
        var transform = CreateTransform();
        var marker = HitTestMarker(point, transform);
        if (marker is not null)
        {
            SelectMarker(marker);
            return;
        }

        var selected = transform.ScreenToGeo(point);
        SelectCoordinate(selected);
    }

    void BeginInertia(Vector pixelsPerSecond)
    {
        if (pixelsPerSecond.Length < 140)
        {
            FinishGesture();
            return;
        }

        _inertiaVelocity = pixelsPerSecond;
        _inertiaTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _inertiaTimer.Tick += OnInertiaTick;
        _inertiaTimer.Start();
    }

    void OnInertiaTick(object? sender, EventArgs e)
    {
        TranslateBy(_inertiaVelocity * 0.016);
        _inertiaVelocity *= 0.9;
        if (_inertiaVelocity.Length >= 24)
            return;

        StopInertia();
        FinishGesture();
    }

    void StopInertia()
    {
        if (_inertiaTimer is null)
            return;

        _inertiaTimer.Stop();
        _inertiaTimer.Tick -= OnInertiaTick;
        _inertiaTimer = null;
        _inertiaVelocity = default;
    }

    void FinishGesture()
    {
        if (_contacts.Count > 0 || _inertiaTimer is not null)
            return;

        _suspendTileRefresh = false;
        _isPanning = false;
        _isPinching = false;
        if (!_tilesStale)
            return;

        _tilesStale = false;
        QueueTileRefresh();
    }

    bool TryPinchPoints(out Point first, out Point second)
    {
        first = default;
        second = default;
        if (_contacts.Count < 2)
            return false;

        using var enumerator = _contacts.Values.GetEnumerator();
        if (!enumerator.MoveNext())
            return false;

        first = enumerator.Current;
        if (!enumerator.MoveNext())
            return false;

        second = enumerator.Current;
        return true;
    }

    static double Distance(Point first, Point second)
    {
        var deltaX = first.X - second.X;
        var deltaY = first.Y - second.Y;
        return global::System.Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    static Point Midpoint(Point first, Point second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    async Task<bool> CopySelectionAsync(
        string? text,
        CancellationToken cancellationToken)
    {
        if (text is null)
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (ClipboardWriter is not null)
            {
                await ClipboardWriter(text, cancellationToken);
                return true;
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
                return false;

            await clipboard.SetTextAsync(text);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    void ApplyInteractionSelection(
        GeoCoordinate coordinate,
        MapMarker? marker)
    {
        var changed = SelectedCoordinate != coordinate
            || !ReferenceEquals(SelectedMarker, marker);
        _suppressSelectionChanged = true;
        try
        {
            SelectedCoordinate = coordinate;
            SelectedMarker = marker;
        }
        finally
        {
            _suppressSelectionChanged = false;
        }

        if (marker is not null)
            MarkerSelected?.Invoke(marker);
        PointSelected?.Invoke(coordinate);
        if (changed)
            SelectionChanged?.Invoke();
    }

    static bool HasEnoughDrawingPoints(GeoDrawingKind kind, int count) =>
        kind switch
        {
            GeoDrawingKind.Point => count >= 1,
            GeoDrawingKind.Circle => count >= 2,
            GeoDrawingKind.Polyline => count >= 2,
            GeoDrawingKind.Polygon => count >= 3,
            _ => false,
        };

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
