# Novolis.Avalonia.Video

Avalonia controls for realtime media frames and a minimal Movie Maker–style preview/storyboard.

## Install

```bash
dotnet add package Novolis.Avalonia.Video
```

## Quick start — live frames

```csharp
var surface = new VideoSurface();
surface.Present(frame); // Novolis.Video.Rtc.VideoFrame
```

## Quick start — storyboard preview

```csharp
var project = new MovieProject("Demo");
var transport = new EditTransport();
var composer = new MoviePreviewComposer();
var surface = new VideoSurface();
var strip = new StoryboardStrip();
strip.Bind(project);

using var session = new MoviePreviewSession(project, transport, composer, surface, strip);
session.Start();
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Video.Edit` | Project / storyboard / transport |
| `Novolis.Video.Rtc` | Mesh session producing frames |
