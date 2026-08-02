# Novolis.Avalonia.Media

Avalonia controls for realtime media frames (`VideoSurface`).

## Install

```bash
dotnet add package Novolis.Avalonia.Media
```

## Quick start

```csharp
var surface = new VideoSurface();
surface.Present(frame); // Novolis.Media.Rtc.VideoFrame
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Media.Rtc.Abstractions` | `VideoFrame` |
| `Novolis.Media.Rtc` | Mesh session producing frames |
