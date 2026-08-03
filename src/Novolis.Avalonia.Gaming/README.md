<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Avalonia.Gaming

Avalonia game shell for **menus and HUD over Silk / TwoD rendering** (code-only, no XAML).

Use this when interactive UI (pause, encyclopedia, options, status strips) must sit above a GL viewport instead of being drawn into `TwoDScene` sprites.

## Install

```bash
dotnet add package Novolis.Avalonia.Gaming
```

**Prerequisites:** Avalonia 12+, .NET 10. `Novolis.Avalonia.Rendering` (and thus `Novolis.Rendering.TwoD` + Silk).

Local multi-repo iteration before GPR has the new build — keep the PackageReference and use ProjectReference mode:

```powershell
pwsh -File d:\novolis\novolis-governance\build\Generate-Platform-Slnx.ps1
dotnet build d:\novolis\<consumer>\path\to\App.csproj -p:NovolisUseProjectReferences=true
```

## Quick start

```csharp
using Novolis.Avalonia.Gaming;
using Novolis.Avalonia.Rendering;
using Novolis.Rendering.TwoD;

var hud = new HudStrip();
hud.SetTexts("WEEK 13", "9280  |  60  |  200", "ENCYCLOPEDIA");

var shell = GameShell.CreateWithTwoD(hud); // HardPause by default
shell.TwoDViewport!.Scene = new TwoDScene();
shell.TwoDViewport.FrameUpdating += (_, e) =>
{
    if (!shell.ShouldAdvanceSimulation())
        return;
    // advance sim with e.DeltaSeconds
};

// Blocking menu — freezes sim under HardPause
shell.ShowModal(new TextBlock { Text = "Paused", FontSize = 24 });

window.Content = shell;
```

Headless / autopilot:

```csharp
shell.PauseMode = GamePauseMode.RunAlways;
```

## API

| Type | Role |
|------|------|
| `GameShell` | Viewport + HUD + modal layers; `ShouldAdvanceSimulation()` |
| `GamePauseMode` | `HardPause` (human session default) / `RunAlways` |
| `GameSimGate` | Pure tick gate (unit-testable) |
| `HudStrip` | Left / center / right status row |
| `ModalMenuHost` | Dimmed menu overlay (Escape / outside click → dismiss) |

## Boundaries

| Package | Role |
|---------|------|
| `Novolis.Avalonia.Rendering` | GL hosts only (`TwoDSceneControl`, CPU frames) |
| **`Novolis.Avalonia.Gaming`** | Avalonia chrome **over** those hosts |
| `Novolis.Avalonia.Studio` | Editor chrome (status/flash/busy) — not game loops |
| `Novolis.Avalonia.Briefing` | Post-run scorecards / feeds |

Game / campaign domain (Rebellion windows, Calypso captain copy) stays in the app. This package is host plumbing only.

## Windows OpenGL

Same as Rendering — prefer WGL so `OpenGlControlBase` initializes:

```csharp
appBuilder.UsePlatformDetect()
    .With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.Wgl] });
```

