# Design

Avalonia controls and shells for CAD, gaming, agents, video, and mobile.

Published docs: [https://novolis-platform.github.io/.github/novolis-avalonia/](https://novolis-platform.github.io/.github/novolis-avalonia/)

## Layer placement

**Avalonia** layer only (`Novolis.Avalonia.*`). Sole libraries allowed to take Avalonia package refs.

## Goals

- Keep public APIs documented and packable as `Novolis.*` on GitHub Packages (when applicable).
- Prefer BCL types and existing Novolis packages over parallel abstractions.
- Document restore and ProjectReference-mode builds without local NuGet folder feeds.

## Non-goals

- Local NuGet folder feeds or committed cross-repo `ProjectReference` into sibling checkouts.
- Avalonia package references outside `Novolis.Avalonia.*`.
- Upward spine dependencies (e.g. Math → Simulation).

## Packages

- `Novolis.Avalonia.3D`
- `Novolis.Avalonia.Agent`
- `Novolis.Avalonia.Agent.Protocol`
- `Novolis.Avalonia.Audio`
- `Novolis.Avalonia.Briefing`
- `Novolis.Avalonia.Cad`
- `Novolis.Avalonia.Cad.Ship`
- `Novolis.Avalonia.Controls`
- `Novolis.Avalonia.Controls.Sketch`
- `Novolis.Avalonia.Gaming`
- `Novolis.Avalonia.Git`
- `Novolis.Avalonia.Layout`
- `Novolis.Avalonia.Live`
- `Novolis.Avalonia.Manuscript`
- `Novolis.Avalonia.Markdown`
- `Novolis.Avalonia.Mermaid`
- `Novolis.Avalonia.Mobile`
- `Novolis.Avalonia.Mobile.Android`
- `Novolis.Avalonia.Mobile.Desktop`
- `Novolis.Avalonia.Packaging.Inno`
- `Novolis.Avalonia.Raylib`
- `Novolis.Avalonia.Rendering`
- `Novolis.Avalonia.StarMap`
- `Novolis.Avalonia.Studio`
- `Novolis.Avalonia.Torrent`
- `Novolis.Avalonia.Video`
- `Novolis.Avalonia.Voice`

## Topics

- `dotnet`
- `avalonia`
- `ui`
- `novolis`
