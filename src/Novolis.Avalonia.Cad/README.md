# Novolis.Avalonia.Cad

Shared Avalonia CAD surface for Draft Studio / CalypsoCad:

- **Primitives** (`Novolis.Cad.Primitives` namespace): `.cadjson` / `.cadphys` DTOs
- **Editor**: plan viewport, Raylib model view, tools, command DSL
- **Preview**: `CadPreviewControl` + `CadViewportExporter` (plan PNG, model PNG, phys)
- **LLM session**: localhost HTTP `:18775` + TCP JSONL `:18776` (`CadSessionSurface` / `CadSessionService`)

UI and HTTP/TCP both call `CadSessionService.Execute` for action parity.
