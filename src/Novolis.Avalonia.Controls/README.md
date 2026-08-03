<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Controls

Reusable Avalonia controls (code-only, no XAML): analyzer views, choice/picker dialogs, marked lists, job queue panels, and an Excalidraw-inspired `SketchControl` (pen/line/spline/box/circle/eraser, meetup vertex snap, gridify).

## Install

```bash
dotnet add package Novolis.Avalonia.Controls
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia. References `Novolis.Avalonia.Layout`.

## Quick start

```csharp
using Novolis.Avalonia.Controls;

// Choice dialog (recovery / conflict patterns)
var id = await ChoiceDialog.ShowAsync(window, "External change", "File changed on disk.", null,
[
    new ChoiceOption("keep", "Keep local", IsDefault: true),
    new ChoiceOption("reload", "Reload disk"),
    new ChoiceOption("compare", "Compare later", IsCancel: true)
]);

// Filtered picker
var chapter = await FilteredPickerDialog.ShowAsync(window, "Go To", chapters, c => c.Title);

// Marked nav rows
var list = MarkedListBox.Create([new MarkedListRow("*", "3", "Quiet Harbor", "420")]);

// Job queue panel with overall + per-item progress
var jobs = new JobQueuePanel();
jobs.SetJobs([
    new JobQueueRow
    {
        Title = "Generate audiobook",
        StatusLabel = "Running",
        Progress = 0.42,
        ProgressLabel = "Synthesizing chapters 3/10",
        ChapterProgress =
        [
            new JobChapterProgress { Label = "Prologue", Progress = 1, StatusLabel = "done" },
            new JobChapterProgress { Label = "Chapter 1", Progress = 0.4, StatusLabel = "2/5" },
        ],
        CanCancel = true
    }
]);
jobs.CancelRequested += row => { /* cancel */ };

// Freehand sketch canvas (pen → selectable/resizable strokes, grid, Gridify)
var sketch = new SketchControl
{
    Tool = SketchTool.Pen,
    GridSize = 20,
    GridVisible = true,
    SnapEnabled = false
};
sketch.GridifySelection(); // snap selected (or all) strokes to the grid
var json = SketchJson.Serialize(sketch.Document!);
```

## SketchControl

| API | Purpose |
|-----|---------|
| `Tool` | `Pen` or `Select` |
| `GridSize` / `GridVisible` / `SnapEnabled` | Resizable grid + optional live snap |
| `GridifySelection()` | Quantize strokes onto the grid (undoable) |
| `Undo()` / `Redo()` / `Clear()` | History and clear |
| `SketchJson` | Serialize/deserialize `SketchDocument` |

Pan with middle mouse or Space+drag; wheel zooms toward the cursor. Select tool shows bounds grips for resize.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Layout` | Analyzer workspace shell and filter bar |
| `Novolis.Avalonia.Studio` | Status/flash chrome and focus mode |

## Support

Pre-release.

