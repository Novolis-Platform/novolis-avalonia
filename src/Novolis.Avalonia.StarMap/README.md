<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.StarMap

Pan/zoom Avalonia control for catalog points, route edges, path highlight, labels, band-styled edges, and an optional marker. Map from `Novolis.Astro.*` at the app layer — this package is UI-only.

## Install

```bash
dotnet add package Novolis.Avalonia.StarMap
```

## Quick start

```csharp
using Novolis.Avalonia.StarMap;

var map = new StarMapControl { ShowChartGrid = true, ShowLabels = true };
map.StarSelected += id => { /* user picked a star */ };

var points = new List<StarMapPoint>
{
    new() { Id = "sol", Label = "Sol", X = 0, Y = 0 },
    new() { Id = "alpha", Label = "Alpha Cen", X = 4.3, Y = -1.2 },
};
var edges = new List<StarMapEdge>
{
    new() { FromId = "sol", ToId = "alpha", BandTag = "primary" },
};

map.SetMap(points, edges);
map.FitToPoints();
map.SetRoute(edges);
map.SetShipMarker(0, 0, visible: true);
```

## API

| API | Purpose |
|-----|---------|
| `StarMapPoint` | `Id`, `Label`, `X`, `Y`, optional `Radius` |
| `StarMapEdge` | `FromId`, `ToId`, optional `BandTag` (styled stroke) |
| `ShowLabels` | Draw point labels (default on) |
| `BandPens` | Optional BandTag → pen map |
| `StarBrush` / `EdgePen` / `RoutePen` / … | Visual overrides |
| `FitToPoints` / `SetCamera` | Camera helpers |
| `WorldSpaceGrid` / `WorldGridStep` | World-unit grid |
| `HitRadius` | Hit-test radius (screen px) |
| `SetMap` / `SetRoute` / `SetShipMarker` | Bind helpers |
| `StarSelected` | Click selection |

## Related / dogfood

| App | Notes |
|-----|-------|
| StarMapLab | Interactive star-map lab |
| GeoPolity / SinsOfACapitalismTycoon | Theatre / bridge map projection |
