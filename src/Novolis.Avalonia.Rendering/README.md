<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-avalonia/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-avalonia/) · [Source](https://github.com/Novolis-Platform/novolis-avalonia)
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Rendering

Avalonia hosts for Novolis rendering stacks (no XAML).

## Install

```bash
dotnet add package Novolis.Avalonia.Rendering
```

**Prerequisites:** Avalonia 12+, .NET 10. Published `Novolis.Rendering.Planar` and `Novolis.Silk.Runtime` on GitHub Packages.

## Quick start

```csharp
using Novolis.Avalonia.Rendering;
using Novolis.Rendering.Presentation;
using Novolis.Rendering.Planar;

var view = new PlanarSceneControl { Scene = new PlanarScene() };
view.FrameUpdating += (_, e) =>
{
    // Poll-style (Silk-like): edge-triggered in framebuffer pixels
    if (view.IsMouseButtonPressed(MouseButton.Left))
        HitTest(view.MousePixelPosition);
};
view.ScenePointerPressed += (_, e) =>
{
    // Event-style: e.PixelX/Y already DPI-scaled to the GL framebuffer
    if (e.Button == MouseButton.Left && e.IsInside)
        HitTest(new(e.PixelX, e.PixelY));
};

// OpenGlControlBase needs ICustomHitTest (implemented) or pointer events never arrive.
// Agents can also inject: view.InjectPointerPressed(px, py);
```

## Controls

| Control | Hosts | Use when |
|---------|-------|----------|
| `PlanarSceneControl` | `Novolis.Rendering.Planar` + Silk OpenGL | World / map / sprite viewport |
| `Rgba32FrameControl` | CPU `Rgba32` buffer (`IFramePresenter`) | Path tracing preview, software ray trace |

For Avalonia menus and HUD **over** the Silk surface (pause, encyclopedia, status strips), use **`Novolis.Avalonia.Gaming`** (`GameShell`) instead of drawing interactive chrome into `PlanarScene`.

## Planar scene (OpenGL)

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
- For Raylib inside Avalonia, use **`Novolis.Avalonia.Raylib`** (`RaylibHostControl`).
- For interactive HUD/menus over Planar, use **`Novolis.Avalonia.Gaming`** (`GameShell`).

