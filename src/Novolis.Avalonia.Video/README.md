<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
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
surface.Present(rtcFrame); // Novolis.Video.Rtc.VideoFrame
surface.Present(rawFrame); // Novolis.Video.RawVideoFrame, letterboxed
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

