# Novolis.Avalonia.Rendering

Avalonia hosts for Novolis rendering stacks (no XAML).

## Packages

```bash
dotnet add package Novolis.Avalonia.Rendering
```

**Prerequisites:** Avalonia 12+, .NET 10. Published `Novolis.Rendering.TwoD` and `Backends.TwoD.Silk` on GitHub Packages.

## Controls

| Control | Hosts | Use when |
|---------|-------|----------|
| `TwoDSceneControl` | `Novolis.Rendering.TwoD` + Silk OpenGL | Platformers, RTS orthographic UI, HUD/menus |
| `Rgba32FrameControl` | CPU `Rgba32` buffer (`IFramePresenter`) | Path tracing preview, software ray trace |

## TwoD scene (OpenGL)

```csharp
using Novolis.Avalonia.Rendering;
using Novolis.Rendering.TwoD;

var view = new TwoDSceneControl { Scene = new TwoDScene() };
view.FrameUpdating += (_, e) => { /* game logic */ };
```

On **Windows**, prefer WGL so `OpenGlControlBase` initializes:

```csharp
appBuilder.UsePlatformDetect()
    .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] });
```

On **macOS** (Avalonia 12 defaults to Metal), force OpenGL if required:

```csharp
.With(new AvaloniaNativePlatformOptions
{
    RenderingMode = [AvaloniaNativeRenderingMode.OpenGl],
});
```

## CPU frame (path trace)

```csharp
var frame = new Rgba32FrameControl();
frame.PresentCpuFrame(pixels, width, height);
// or: backend presents via IFramePresenter when frame is the control
```

## Boundaries

- References **Rendering** packages only — not Simulation, Physics, or Raylib.
- Apps wire simulation → scene or `PresentCpuFrame` at compose time.
