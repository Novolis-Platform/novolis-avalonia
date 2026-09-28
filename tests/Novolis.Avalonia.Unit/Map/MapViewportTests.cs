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
}
