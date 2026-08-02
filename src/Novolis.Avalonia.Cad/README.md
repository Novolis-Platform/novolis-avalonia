# Novolis.Avalonia.Cad

Shared Avalonia CAD surface for **Draft Studio**, **Novolis CAD Studio 3D**, and preview hosts:

- **Primitives**: PackageReference `Novolis.Cad.Primitives` (`.cadjson` / `.cadphys` DTOs — Avalonia-free)
- **Bridge**: PackageReference `Novolis.Cad.SceneBridge` (`exportscene` / `bridgescene` → `.nov3djson`)
- **Workspaces**: **CAD** (exact solids/sketches) · **Modeling** (mesh modifiers) · **Preview** (materials/lights/cameras)
- **Scene**: shared hierarchy (`CadSceneTree`) with generators, Mesh From Solid adapters, and modifier stacks
- **Editor**: plan viewport, Raylib 3D host, tools, command DSL, workspace chrome
- **Preview**: `CadPreviewControl` + `CadViewportExporter` (plan PNG, model PNG, phys)
- **LLM session**: localhost HTTP `:18775` + TCP JSONL `:18776` (`CadSessionSurface` / `CadSessionService`)

## Agent parity

UI chrome and the LLM call the **same** `CadSessionService.Execute` path. Pointer tools (Wall/Line/…) have parametric twins (`addwall` / `addline` / …). Studio host modes use `setstudioworkspace` (`draft2d` \| `draft3d` \| `model` \| `stage`).

**Command DSL** (`runcommand` / command bar) — AutoCAD-ish scripts with nested `Point` and `;` chains:

```text
Line(Point(0.0,1.0), Point(1.0,1.0)); Circle(Point(2,2), 0.5); Extrude(2.4); Material("Concrete");
```

Also: `new → setstudioworkspace draft2d → addrect → extrudeprofile → setmaterial → exportscene / bridgescene`

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

## API

| API | Purpose |
|-----|---------|
| `CadEditorSurface` | Host CAD / Modeling / Preview workspaces on a shared scene |
| `CadEditorSurface.SetWorkspace(CadWorkspace)` | Switch CAD, Modeling, or Preview mode |
| `CadSessionService.Execute(command)` | Shared command path for UI chrome and LLM session |
| `CadSessionSurface` | Localhost HTTP `:18775` + TCP JSONL `:18776` session host |
| `CadPreviewControl` | Plan/model preview viewport |
| `CadViewportExporter` | Export plan PNG, model PNG, `.cadphys` |
| `CadSceneTree` | Shared hierarchy with generators and modifier stacks |
| `CadDraft3DViewport` | Raylib 3D host for draft/model stages |

## Related / dogfood

| Package / app | Notes |
|---------------|-------|
| [`Novolis.Cad.Primitives`](../../../novolis-cad/src/Novolis.Cad.Primitives/README.md) | `.cadjson` / `.cadphys` DTOs |
| [`Novolis.Cad.SceneBridge`](../../../novolis-cad/src/Novolis.Cad.SceneBridge/README.md) | `exportscene` / `bridgescene` → `.nov3djson` |
| [`Novolis.Avalonia.3D`](../Novolis.Avalonia.3D/README.md) | OpenGL scene editor surface |
| [Draft Studio](../../../novolis-apps/src/DraftStudio) | 2D/3D CAD studio |
