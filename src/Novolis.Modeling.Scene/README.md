# Novolis.Modeling.Scene

Mesh-first scene graph for lightweight C4D-inspired modeling (Object Manager hierarchy).

- Typed nodes: Group, Mesh, Generator, Modifier, Material, Light, Camera, Null
- Primitives tessellated to `EditableMesh` (`PrimitiveMesher`)
- Generators: Cloner/Array, Symmetry, Boole via `Novolis.Math.Geometry.MeshBoolean`
- Modifiers: Weld, Optimize, Subdivision, Extrude, Bevel, Bridge
- `.nov3djson` load/save (`format: novolis.scene`)

No Avalonia UI and no LLM transports — see `Novolis.Avalonia.3D` and `Novolis.Agent.Surface`.
