# Novolis.Avalonia.Timeline

Git-graph timeline Avalonia controls for studio apps. Renders `Novolis.Timeline.Presentation.GitGraph` rows with scroll, branch legend, empty state, and restore-on-double-click.

## Install

```bash
dotnet add package Novolis.Avalonia.Timeline
dotnet add package Novolis.Timeline.Presentation
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia.

## Quick start

```csharp
using Novolis.Avalonia.Timeline;
using Novolis.Timeline.Presentation.GitGraph;

var panel = new GitHistoryPanel();
panel.SetRows(rows); // IReadOnlyList<GitGraphTimelineRow>
panel.RestoreRequested += (_, row) => RestoreSnapshot(row.Id);
panel.SelectionChanged += (_, _) =>
{
    var selected = panel.SelectedRow;
};
```

Build rows with `GitGraphTimelineBuilder.Build(tree, nodes, branches, head)` from `Novolis.Timeline.Presentation`.

## API

| API | Purpose |
|-----|---------|
| `GitHistoryPanel` | Bordered panel with scroll, branch legend, empty-state hint |
| `GitHistoryPanel.SetRows(rows)` | Bind timeline rows; shows empty UI when count is 0 |
| `GitHistoryPanel.SelectedRow` | Current `GitGraphTimelineRow?` |
| `GitHistoryPanel.SelectionChanged` | Forwards `ListBox` selection events |
| `GitHistoryPanel.RestoreRequested` | Fired when user double-clicks a row |
| `GitGraphTimelineList` | Standalone `ListBox` with git-log-style lane rendering |
| `GitGraphTimelineList.SetRows(rows)` | Bind rows (skips refresh if equivalent) |
| `GitGraphTimelineList.SelectHeadRow(rows)` | Selects row where `IsHere == true` |
| `GitGraphTimelineList.SelectedGitRow` | Typed selected row |
| `GitGraphTimelineList.RestoreRequested` | Double-tap restore event |

Row model (`GitGraphTimelineRow`) lives in `Novolis.Timeline.Presentation.GitGraph` — fields include `Id`, `Graph`, `Subject`, `BranchName`, `IsHere`, `IsBranchPoint`.

## Related / dogfood

| App / package | Notes |
|---------------|-------|
| `Novolis.Timeline.Presentation` | Row builder and graph model (required dependency) |
| [MeshBench](../../../novolis-dogfooding/apps/rendering/MeshBench) | Git history panel in rendering studio |
