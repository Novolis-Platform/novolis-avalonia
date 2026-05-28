# Novolis.Avalonia.Raylib

Embeds a **Raylib** viewport inside Avalonia via a hidden GLFW window and RGBA framebuffer streaming.

## Install

```bash
dotnet add package Novolis.Avalonia.Raylib
```

**Prerequisites:** Avalonia 12+, .NET 10, `Novolis.Raylib.Runtime` (native raylib per RID).

## Quick start

```csharp
using Novolis.Avalonia.Raylib;
using Novolis.Raylib.Rendering;

var host = new RaylibHostControl
{
    FrameWidth = 640,
    FrameHeight = 480,
    MinHeight = 320,
};

host.FrameRendering += (_, e) =>
{
    Graphics.ClearBackground(Colors.RayWhite);
    Graphics.DrawText("Hello from Raylib inside Avalonia", 24, 24, 24, Colors.DarkGray);
};
```

`FrameRendering` runs on the **Raylib render thread** — only call Raylib draw APIs there.

## Control

| Member | Purpose |
|--------|---------|
| `RaylibHostControl` | Streams Raylib frames into an Avalonia `Image` (stretch fill) |
| `FrameWidth` / `FrameHeight` | Internal render resolution (scaled to control bounds) |
| `TargetFps` | Raylib loop target FPS (default 60) |
| `FrameRendering` | Per-frame draw hook (`RaylibFrameEventArgs`) |
| `IsHostRunning` | Whether the embedded loop is active |

## Boundaries

- Avalonia ↔ Raylib glue only — no Simulation or Physics references.
- Use `Novolis.Avalonia.Rendering` for OpenGL TwoD or CPU path-trace hosts when Raylib is not required.
