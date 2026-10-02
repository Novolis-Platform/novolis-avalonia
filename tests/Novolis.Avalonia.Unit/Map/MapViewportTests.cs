using Avalonia;
using Novolis.Avalonia.Map;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Unit.Map;

public sealed class MapViewportTests
{
    [Test]
    public async Task Geographic_and_screen_coordinates_round_trip()
    {
        var center = new GeoCoordinate(58.14623, 7.99517);
        var transform = new MapViewportTransform(
            new MapViewport(center, 12),
            800,
            600);

        var point = new GeoCoordinate(58.15, 8.01);
        var centerScreen = transform.GeoToScreen(center);
        var screen = transform.GeoToScreen(point);
        var roundTrip = transform.ScreenToGeo(screen);

        await Assert.That(centerScreen.X).IsEqualTo(400d).Within(1e-9);
        await Assert.That(centerScreen.Y).IsEqualTo(300d).Within(1e-9);
        await Assert.That(roundTrip.Latitude).IsEqualTo(point.Latitude).Within(1e-9);
        await Assert.That(roundTrip.Longitude).IsEqualTo(point.Longitude).Within(1e-9);
    }

    [Test]
    public async Task Longitude_wrap_keeps_antimeridian_points_nearby()
    {
        var transform = new MapViewportTransform(
            new MapViewport(new GeoCoordinate(0, 179.9), 8),
            800,
            600);

        var screen = transform.GeoToScreen(new GeoCoordinate(0, -179.9));

        await Assert.That(screen.X).IsLessThan(500);
        await Assert.That(screen.X).IsGreaterThan(300);
    }

    [Test]
    public async Task Zoom_anchor_preserves_the_geographic_point_under_the_pointer()
    {
        var initial = new MapViewport(new GeoCoordinate(58.14623, 7.99517), 10);
        var transform = new MapViewportTransform(initial, 800, 600);
        var pointer = new Point(675, 184);
        var anchor = transform.ScreenToGeo(pointer);
        var zoomed = new MapViewport(initial.Center, 14);
        var center = MapViewportTransform.CenterForAnchor(
            zoomed,
            transform.Width,
            transform.Height,
            anchor,
            pointer);
        var zoomedTransform = new MapViewportTransform(
            new MapViewport(center, 14),
            800,
            600);
        var result = zoomedTransform.GeoToScreen(anchor);

        await Assert.That(result.X).IsEqualTo(pointer.X).Within(1e-6);
        await Assert.That(result.Y).IsEqualTo(pointer.Y).Within(1e-6);
    }

    [Test]
    public async Task Visible_tiles_normalize_wrapped_x_and_clip_y()
    {
        var key = new MapTileKey(3, -1, 2);
        var transform = new MapViewportTransform(
            new MapViewport(new GeoCoordinate(0, 179), 3),
            512,
            512);
        var visible = transform.GetVisibleTileKeys();

        await Assert.That(key.X).IsEqualTo(7);
        await Assert.That(visible).IsNotEmpty();
        await Assert.That(visible.All(tile => tile.Zoom == 3 && tile.Y is >= 0 and < 8)).IsTrue();
    }

    [Test]
    public async Task Map_control_accepts_viewport_and_selection_state()
    {
        var map = new MapControl();
        var point = new GeoCoordinate(58.14623, 7.99517);

        map.SetViewport(point, 14);
        map.SelectedCoordinate = point;

        await Assert.That(map.Viewport.Center).IsEqualTo(point);
        await Assert.That(map.Viewport.Zoom).IsEqualTo(14d);
        await Assert.That(map.SelectedCoordinate).IsEqualTo(point);
    }

    [Test]
    public async Task Map_control_exposes_reusable_track_and_status_state()
    {
        var map = new MapControl();
        var first = new GeoCoordinate(58.14, 7.99);
        var second = new GeoCoordinate(58.15, 8.01);

        map.Tracks = [new MapTrackOverlay("day", [first, second], "Day samples")];
        map.ErrorMessage = "Tiles unavailable";

        await Assert.That(map.Tracks).Count().IsEqualTo(1);
        await Assert.That(map.Tracks![0].Points).Count().IsEqualTo(2);
        await Assert.That(map.ErrorMessage).IsEqualTo("Tiles unavailable");
    }

    [Test]
    public async Task Fit_to_content_centers_and_clamps_the_viewport()
    {
        var map = new MapControl();
        map.Measure(new Size(800, 600));
        map.Arrange(new Rect(0, 0, 800, 600));
        var first = new GeoCoordinate(58.14, 7.99);
        var second = new GeoCoordinate(58.15, 8.01);

        map.FitToContent([first, second]);

        await Assert.That(map.Viewport.Center.Latitude).IsEqualTo(58.145d).Within(1e-9);
        await Assert.That(map.Viewport.Center.Longitude).IsEqualTo(8d).Within(1e-9);
        await Assert.That(map.Viewport.Zoom).IsGreaterThan(MapViewport.MinimumZoom);
        await Assert.That(map.Viewport.Zoom).IsLessThan(MapViewport.MaximumZoom);
    }

    [Test]
    public async Task Dragging_right_moves_the_center_coordinate_right_on_screen()
    {
        var center = new GeoCoordinate(58.14623, 7.99517);
        var start = new MapViewport(center, 14);
        var transform = new MapViewportTransform(start, 800, 600);

        var shifted = new MapViewportTransform(transform.Translate(new Vector(120, -40)), 800, 600);
        var screen = shifted.GeoToScreen(center);

        await Assert.That(screen.X).IsEqualTo(520d).Within(1e-6);
        await Assert.That(screen.Y).IsEqualTo(260d).Within(1e-6);
        await Assert.That(shifted.Viewport.Zoom).IsEqualTo(14d);
    }

    [Test]
    public async Task Pinch_doubles_distance_and_keeps_the_anchor_under_the_fingers()
    {
        var center = new GeoCoordinate(58.14623, 7.99517);
        var start = new MapViewport(center, 10);
        var before = new MapViewportTransform(start, 800, 600);
        var midpoint = new Point(540, 220);
        var anchor = before.ScreenToGeo(midpoint);

        var pinched = MapViewportTransform.Pinch(
            start,
            800,
            600,
            anchor,
            new Point(560, 250),
            startDistance: 100,
            distance: 200);
        var after = new MapViewportTransform(pinched, 800, 600);
        var screen = after.GeoToScreen(anchor);

        await Assert.That(pinched.Zoom).IsEqualTo(11d).Within(1e-9);
        await Assert.That(screen.X).IsEqualTo(560d).Within(1e-6);
        await Assert.That(screen.Y).IsEqualTo(250d).Within(1e-6);
    }

    [Test]
    public async Task Two_finger_pan_follows_the_midpoint_without_changing_zoom()
    {
        var center = new GeoCoordinate(58.14623, 7.99517);
        var start = new MapViewport(center, 12);
        var before = new MapViewportTransform(start, 800, 600);
        var startMidpoint = new Point(400, 300);
        var anchor = before.ScreenToGeo(startMidpoint);

        var panned = MapViewportTransform.Pinch(
            start,
            800,
            600,
            anchor,
            new Point(470, 260),
            startDistance: 140,
            distance: 140);
        var after = new MapViewportTransform(panned, 800, 600);
        var screen = after.GeoToScreen(anchor);

        await Assert.That(panned.Zoom).IsEqualTo(12d);
        await Assert.That(screen.X).IsEqualTo(470d).Within(1e-6);
        await Assert.That(screen.Y).IsEqualTo(260d).Within(1e-6);
    }
}
