# Novolis.Avalonia.Cad

Shared Avalonia CAD surface for Draft Studio and preview hosts:

- **Primitives** (`Novolis.Cad.Primitives` namespace): `.cadjson` / `.cadphys` DTOs
- **Workspaces**: **CAD** (exact solids/sketches) · **Modeling** (mesh modifiers) · **Preview** (materials/lights/cameras)
- **Scene**: shared hierarchy (`CadSceneTree`) with generators, Mesh From Solid adapters, and modifier stacks
- **Editor**: plan viewport, Raylib 3D host, tools, command DSL, workspace chrome
- **Preview**: `CadPreviewControl` + `CadViewportExporter` (plan PNG, model PNG, phys)
- **LLM session**: localhost HTTP `:18775` + TCP JSONL `:18776` (`CadSessionSurface` / `CadSessionService`)

UI and HTTP/TCP both call `CadSessionService.Execute` for action parity.
