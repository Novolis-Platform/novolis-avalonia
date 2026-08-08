<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Controls

Reusable Avalonia interaction atoms (code-only, no XAML): choice/picker dialogs, marked lists, job queue panels, sortable data grids, hex dump, and detail trees.

Sketch canvas lives in [`Novolis.Avalonia.Controls.Sketch`](../Novolis.Avalonia.Controls.Sketch/README.md). Torrent chrome lives in [`Novolis.Avalonia.Torrent`](../Novolis.Avalonia.Torrent/README.md).

## Install

```bash
dotnet add package Novolis.Avalonia.Controls
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia.

## Control grain

Keep atoms (lists, dialogs, job rows). Reject controls that embed multiple product jobs or open workspaces. See [avalonia-composition-grain.md](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/avalonia-composition-grain.md).

## Quick start

```csharp
using Novolis.Avalonia.Controls;

var id = await ChoiceDialog.ShowAsync(window, "External change", "File changed on disk.", null,
[
    new ChoiceOption("keep", "Keep local", IsDefault: true),
    new ChoiceOption("reload", "Reload disk"),
    new ChoiceOption("compare", "Compare later", IsCancel: true)
]);

var chapter = await FilteredPickerDialog.ShowAsync(window, "Go To", chapters, c => c.Title);

var list = MarkedListBox.Create([new MarkedListRow("*", "3", "Quiet Harbor", "420")]);

var jobs = new JobQueuePanel();
jobs.SetJobs([
    new JobQueueRow
    {
        Title = "Generate audiobook",
        StatusLabel = "Running",
        Progress = 0.42,
        ProgressLabel = "Synthesizing steps 3/10",
        StepProgress =
        [
            new JobStepProgress { Label = "Prologue", Progress = 1, StatusLabel = "done" },
            new JobStepProgress { Label = "Chapter 1", Progress = 0.4, StatusLabel = "2/5" },
        ],
        CanCancel = true
    }
]);

var grid = new SortableDataGrid();
grid.SetColumns([SortableDataGrid.TextColumn("Name", "Name")]);
```

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Avalonia.Controls.Sketch` | Freehand sketch canvas |
| `Novolis.Avalonia.Torrent` | Torrent session panel |
| `Novolis.Avalonia.Layout` | Analyzer / authoring shells |
| `Novolis.Avalonia.Studio` | Status/flash chrome and focus mode |

## Support

Pre-release.
