# Novolis.Avalonia.3D

Lightweight C4D-inspired Avalonia modeling surface (Object Manager + mesh tools):

- Primitives: Box, Sphere, Cylinder, Cone, Plane, Capsule, Torus
- Generators: Array/Cloner, Symmetry, Boole (Union/Difference/Intersection)
- Shaping: Extrude, Bevel, Weld, Optimize, Subdivision
- Look: materials, Omni/Spot/Infinite/Area lights, cameras
- Mutations via `SceneSessionService.Execute` (UI + LLM parity)
- Agent: `AgentSurface.AttachAll` — HTTP `:18785`, TCP `:18786`

Depends on `Novolis.Modeling.Scene` and `Novolis.Agent.Surface`.
