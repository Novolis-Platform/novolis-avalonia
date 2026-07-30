# Novolis.Avalonia.3D

Avalonia CAD 3D editor / renderer surface:

- Primitives: Box, Sphere, Cylinder, Cone, Plane, Capsule, Torus, …
- Generators: Array, Symmetry, Boolean (Union/Difference/Intersection)
- Mesh tools: Extrude, Bevel, Weld, Optimize, Subdivision, …
- Lights & cameras: Point/Spot/Directional/Area, materials, cameras
- Mutations via `SceneSessionService.Execute` (UI + LLM parity)
- Agent: `AgentSurface.AttachAll` — HTTP `:18785`, TCP `:18786`

Depends on `Novolis.Modeling.Scene` and `Novolis.Agent.Surface`.
