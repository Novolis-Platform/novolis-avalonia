# Novolis.Avalonia.3D

C4D-inspired (CinemaLight) Avalonia modeling surface:

- **Object Manager** + property inspector + Raylib viewport with light/camera gizmos
- Light set: Omni, Spot, Infinite, Area
- Mutations via `SceneSessionService.Execute` (UI + LLM parity)
- Agent attach: `AgentSurface.AttachAll(session, SceneSessionContract.Definition)` — HTTP `:18785`, TCP `:18786`

Depends on `Novolis.Modeling.Scene` and `Novolis.Agent.Surface`.
