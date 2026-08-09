<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Controls.Sketch

Excalidraw-inspired `SketchControl` (pen/line/spline/box/circle/eraser, meetup vertex snap, gridify) and document/json model.

## Install

```bash
dotnet add package Novolis.Avalonia.Controls.Sketch
```

## Quick start

```csharp
using Novolis.Avalonia.Controls.Sketch;

var sketch = new SketchControl
{
    Tool = SketchTool.Pen,
    GridSize = 20,
    GridVisible = true,
};
var json = SketchJson.Serialize(sketch.Document!);
```

## Related

| Package | Notes |
|---------|-------|
| `Novolis.Avalonia.Controls` | Dialogs, lists, job queue atoms |
| SketchStudio / SketchLab | Product / dogfood hosts |
