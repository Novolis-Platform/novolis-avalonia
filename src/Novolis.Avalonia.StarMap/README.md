# Novolis.Avalonia.StarMap

Pan/zoom Avalonia control for stellar catalog points, route edges, path highlight, and an optional ship marker. Map from `Novolis.Astro.*` at the app layer — this package is UI-only.

## Install

```bash
dotnet add package Novolis.Avalonia.StarMap
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia.

## Quick start

```csharp
using Novolis.Avalonia.StarMap;

var map = new StarMapControl { ShowChartGrid = true };
map.StarSelected += id => { /* user picked a star */ };

var points = new List<StarMapPoint>
{
    new() { Id = "sol", Label = "Sol", X = 0, Y = 0 },
    new() { Id = "alpha", Label = "Alpha Cen", X = 4.3, Y = -1.2 },
};
var edges = new List<StarMapEdge>
{
    new() { FromId = "sol", ToId = "alpha" },
};

map.SetMap(points, edges);
map.SetRoute(edges); // highlighted path overlay
map.SetShipMarker(0, 0, visible: true);
```

## API

| API | Purpose |
|-----|---------|
| `StarMapPoint` | Model: `Id`, `Label`, `X`, `Y` |
| `StarMapEdge` | Model: `FromId`, `ToId`, optional `BandTag` |
| `StarMapControl` | Pan/zoom `Control` with wheel zoom and drag pan |
| `StarMapControl.Points` / `Edges` / `HighlightedEdges` | Stars, full network, path overlay |
| `StarMapControl.SelectedId` | Currently selected star id |
| `StarMapControl.ShipWorldX` / `ShipWorldY` / `ShipVisible` | “You are here” marker |
| `StarMapControl.FieldBrush` / `ShowChartGrid` | Visual tuning |
| `StarMapControl.SetMap(points, edges?)` | Bind points + optional edges |
| `StarMapControl.SetRoute(routeEdges?)` | Set or clear highlighted path |
| `StarMapControl.SetShipMarker(worldX, worldY, visible)` | Position ship marker |
| `StarMapControl.StarSelected` | `Action<string>` when user clicks a star |

## Related / dogfood

| App | Notes |
|-----|-------|
| [StarMapLab](../../../novolis-dogfooding/apps/astro/StarMapLab) | Interactive star-map lab |
| [SinsOfACapitalismTycoon](../../../novolis-apps/src/SinsOfACapitalismTycoon) | Desk map projection over `StarMapControl` |
