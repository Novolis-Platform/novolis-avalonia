<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

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
| `MovieEditWorkspace` | Full edit shell |
| `MediaLibraryControl` | Thumbnail library + preview + add-to-timeline |
| `TransitionInspectorControl` | Fade/Wipe editor for selected clip |
| `MovieEditTasksPane` | Task button column (events) |
| `MovieMonitorControl` | `VideoSurface` + `EditTransportBar` |
| `StoryboardPane` / `StoryboardStrip` | Storyboard (transition wedges) |
| `EditTransportBar` | Rewind / play-pause |
| `MovieEditPane` | Titled panel chrome |
| `MoviePreviewSession` | Transport → composer → surface loop |

`MovieEditWorkspace.ExportTo` / **Export movie…** writes playable `movie.avi` (+ `audio.wav` + `movie.json`).

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Video.Edit` | Project / storyboard / transport |
| `Novolis.Video.Rtc` | Mesh session producing frames |

