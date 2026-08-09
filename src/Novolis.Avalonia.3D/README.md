<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.3D

Avalonia CAD 3D editor / renderer surface.

## Install

```bash
dotnet add package Novolis.Avalonia.3D
```

## Quick start

```csharp
using Novolis.Avalonia._3D;
using Novolis.Avalonia._3D.Ui;

// OpenGL is the product default for CAD / interactive 3D.
var surface = new SceneEditorSurface();
```

On Windows hosts, prefer WGL so `OpenGlControlBase` initializes:

```csharp
.With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] })
```

**Viewport:** OpenGL (`SceneViewportBackendKind.OpenGl`) is the product default for CAD and interactive 3D. CPU / Vulkan / Raylib presenters are for `ViewportBench` compare and fallbacks only.

- Primitives: Box, Sphere, Cylinder, Cone, Plane, Capsule, Torus, …
- Generators: Array, Symmetry, Boolean (Union/Difference/Intersection)
- Mesh tools: Extrude, Bevel, Weld, Optimize, Subdivision, …
- Lights & cameras: Point/Spot/Directional/Area, materials, cameras
- Mutations via `SceneSessionService.Execute` (UI + LLM parity)
- Agent: `AgentSurface.AttachAll` — HTTP `:18785`, TCP `:18786`
- Stage/render actions: `setmeshmaterial`, `ensurestudiolights`, `openshaderender`, `saverenderpng`, plus `describescene` / `dumpviewport`

**CAD Studio 3D** attaches this surface beside Cad (`:18775`). Typical LLM finish:

```text
ensurestudiolights → setactivecamera → matchviewport → saverenderpng → dumpviewport
```

Depends on `Novolis.3D.Scene` and `Novolis.Agent.Surface`.

