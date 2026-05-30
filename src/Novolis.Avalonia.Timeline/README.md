# Novolis.Avalonia.Timeline

Git-graph timeline controls for Avalonia studio apps (`GitHistoryPanel`, `GitGraphTimelineList`).

## Install

```bash
dotnet add package Novolis.Avalonia.Timeline
dotnet add package Novolis.Timeline.Presentation
```

## Quick start

```csharp
using Novolis.Avalonia.Timeline;
using Novolis.Timeline.Presentation.GitGraph;

var panel = new GitHistoryPanel();
panel.SetRows(GitGraphTimelineBuilder.Build(tree, nodes, branches, head));
panel.RestoreRequested += (_, row) => RestoreTo(row.Id);
```
