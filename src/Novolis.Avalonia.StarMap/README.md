# Novolis.Avalonia.StarMap

Pan/zoom Avalonia control for stellar points and route edges. Map from `Novolis.Astro.*` at the app layer.

## Install

```bash
dotnet add package Novolis.Avalonia.StarMap
```

## Quick start

```csharp
using Novolis.Avalonia.StarMap;

var map = new StarMapControl();
map.SetSystems(points);
map.SetRoute(routeEdges);
```
