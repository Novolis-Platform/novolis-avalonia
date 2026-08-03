# Novolis.Avalonia.Video

Avalonia controls for realtime media frames and reusable Movie Maker–style edit chrome.

## Install

```bash
dotnet add package Novolis.Avalonia.Video
```

## Quick start — live frames

```csharp
var surface = new VideoSurface();
surface.Present(frame); // Novolis.Video.Rtc.VideoFrame
```

## Quick start — full edit workspace

```csharp
var project = new MovieProject("Demo");
using var workspace = new MovieEditWorkspace(project);
window.Content = workspace;
```

## Reusable parts

| Control | Role |
|---------|------|
| `MovieEditWorkspace` | Full tasks / collections / monitor / storyboard shell |
| `MovieEditTasksPane` | Task button column (events) |
| `MediaCollectionsControl` | Asset collections list |
| `MovieMonitorControl` | `VideoSurface` + `EditTransportBar` |
| `StoryboardPane` / `StoryboardStrip` | Scrollable storyboard |
| `EditTransportBar` | Rewind / play-pause |
| `MovieEditPane` | Titled panel chrome |
| `MoviePreviewSession` | Transport → composer → surface loop |

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Video.Edit` | Project / storyboard / transport |
| `Novolis.Video.Rtc` | Mesh session producing frames |
