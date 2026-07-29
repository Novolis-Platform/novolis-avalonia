# Novolis.Avalonia.Cad

Shared Avalonia CAD surface for Draft Studio and preview hosts:

- **Primitives**: PackageReference `Novolis.Cad.Primitives` (`.cadjson` / `.cadphys` DTOs — Avalonia-free)
- **Workspaces**: **CAD** (exact solids/sketches) · **Modeling** (mesh modifiers) · **Preview** (materials/lights/cameras)
- **Scene**: shared hierarchy (`CadSceneTree`) with generators, Mesh From Solid adapters, and modifier stacks
- **Editor**: plan viewport, Raylib 3D host, tools, command DSL, workspace chrome
- **Preview**: `CadPreviewControl` + `CadViewportExporter` (plan PNG, model PNG, phys)
- **LLM session**: localhost HTTP `:18775` + TCP JSONL `:18776` (`CadSessionSurface` / `CadSessionService`)

UI and HTTP/TCP both call `CadSessionService.Execute` for action parity.

## Install

```bash
dotnet add package Novolis.Avalonia.Cad
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Avalonia. References `Novolis.Cad.Primitives`, `Novolis.Math.Geometry`, and related Avalonia packages.

## Quick start

```csharp
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Cad.Ui;

// Host CAD / Modeling / Preview on a shared scene
editor.SetWorkspace(CadWorkspace.Modeling);

// Session parity for UI + LLM (setworkspace / generators / modifiers)
var result = session.Execute(command);
```
