# Novolis.Avalonia.Controls

Reusable Avalonia controls (code-only, no XAML): analyzer views, choice/picker dialogs, marked lists, and job queue panels.

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
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Layout` | Analyzer workspace shell and filter bar |
| `Novolis.Avalonia.Studio` | Status/flash chrome and focus mode |

## Support

Pre-release.
