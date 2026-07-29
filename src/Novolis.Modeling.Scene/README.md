# Novolis.Modeling.Scene

Mesh-first scene graph for C4D-inspired modeling (Object Manager hierarchy).

- Typed nodes: Group, Mesh, Generator, Modifier, Material, Light, Camera, Null
- Light kinds: Omni, Spot, Infinite, Area
- Staged evaluation with narrow invalidation
- `.nov3djson` load/save (`format: novolis.scene`)

No Avalonia UI and no LLM transports — see `Novolis.Avalonia.3D` and `Novolis.Agent.Surface`.
