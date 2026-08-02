# Novolis.Avalonia.Video

Avalonia controls for realtime media frames (`VideoSurface`).

## Install

```bash
dotnet add package Novolis.Avalonia.Video
```

## Quick start

```csharp
var surface = new VideoSurface();
surface.Present(frame); // Novolis.Video.Rtc.VideoFrame
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Video.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Video.Rtc` | Mesh session producing frames |
