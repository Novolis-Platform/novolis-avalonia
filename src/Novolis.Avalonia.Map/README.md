<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Map

Provider-neutral Avalonia map primitives for geographic point selection, pan,
zoom, raster tiles, markers, radius overlays, and visible attribution.

The package intentionally does not choose a tile provider, perform network
requests, persist a cache, or know an application's domain model. An
application supplies an `IMapTileSource` and attribution text.

## Install

```bash
dotnet add package Novolis.Avalonia.Map
```

## Quick start

```csharp
using Novolis.Avalonia.Map;
using Novolis.Math.Geometry;

var map = new MapControl
{
    Viewport = new MapViewport(new GeoCoordinate(58.14623, 7.99517), 14),
    Markers =
    [
        new MapMarker(
            "office",
            new GeoCoordinate(58.14623, 7.99517),
            "Office"),
    ],
    Circles =
    [
        new MapCircleOverlay(
            "office-area",
            new GeoCircle(new GeoCoordinate(58.14623, 7.99517), 200)),
    ],
    Attribution = "© Kartverket",
};

map.PointSelected += coordinate => { /* application handles the selection */ };
```

## API

| API | Purpose |
|-----|---------|
| `MapControl` | Renders tiles and geographic overlays with pan/zoom and selection |
| `MapViewport` | Center and fractional world zoom |
| `MapViewportTransform` | Testable geographic/screen and tile-coordinate conversion |
| `MapMarker` | Selectable geographic point and optional label |
| `MapCircleOverlay` | Geographic radius overlay |
| `IMapTileSource` | Application-owned asynchronous tile provider |
| `RasterMapTileSource` | Decodes `Novolis.IO.Maps` raster bytes into Avalonia tiles |
| `Attribution` | Visible provider attribution text |

Use [Novolis.Math.Geometry](https://github.com/Novolis-Platform/novolis-math)
for `GeoCoordinate`, `GeoCircle`, geodesic distance, and Web Mercator
projection primitives.
