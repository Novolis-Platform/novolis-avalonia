<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Controls.Sketch

Excalidraw-inspired sketch canvas and document model for Avalonia.

| Surface | Link |
|---------|------|
| Wire format | [`sketchjson.md`](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/sketchjson.md) |
| Host docs (tools, shortcuts, architecture, …) | [Sketch Studio docs/](https://github.com/Novolis-Platform/novolis-apps/blob/main/docs/sketch-studio/README.md) |
| Host README | [Sketch Studio](https://github.com/Novolis-Platform/novolis-apps/blob/main/src/SketchStudio/README.md) |
| Unit tests | `Novolis.Avalonia.Unit` / `SketchDocumentTests` |

## Tools (`SketchTool`)

Pen, Select, Line, Spline, Rect, Ellipse, Eraser, SpeechBubble, Text, TextBox.

Notable canvas behaviors (also documented in Sketch Studio):

- **Pen:** meetup vertex snap is skipped mid-stroke so freehand is not yanked to nearby points.
- **Line / Spline:** click vertices; **Enter** completes; **Ctrl+Enter** (or Shift+Enter) closes; **Esc** cancels.
- **Select:** move / resize / rotate grip; **Shift** multi-select; **Ctrl+A** select all; **Space+drag** pan.
- **Fuse / Ungroup:** shared `groupId` on elements (`Ctrl+G` / `Ctrl+Shift+G` in the host).

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
    SnapEnabled = true,
    MeetupEnabled = true,
};
sketch.DocumentChanged += () => { /* persist or dirty flag */ };
var json = SketchJson.Serialize(sketch.Document!);
sketch.Document = SketchJson.Deserialize(json);
```

## Persistence

Use `SketchJson.Serialize` / `Deserialize` for `.sketchjson`. Selection and undo history are **not** written to disk. See the format doc for `kind`, rotation, groups, text, and images.

## Related

| Package / app | Notes |
|---------------|-------|
| `Novolis.Avalonia.Controls` | Dialogs (`ChoiceDialog`), lists, job queue atoms |
| [Sketch Studio docs/](https://github.com/Novolis-Platform/novolis-apps/blob/main/docs/sketch-studio/README.md) | Product host documentation tree |
| SketchLab | Dogfood smoke host |
