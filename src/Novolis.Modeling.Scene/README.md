# Novolis.Modeling.Scene

Mesh-first scene graph for hard-surface modeling (Object Manager hierarchy).

- Typed nodes: Group, Mesh, Generator, Modifier, Material, Light, Camera, Null
- Light kinds: Omni, Spot, Infinite, Area
- Staged evaluation with narrow invalidation
- `.nov3djson` load/save (`format: novolis.scene`)

No Avalonia UI and no LLM transports — see `Novolis.Avalonia.3D` and `Novolis.Agent.Surface`.

## Install

```bash
dotnet add package Novolis.Modeling.Scene
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Modeling.Scene;

var doc = SceneDocument.CreateEmpty("Demo");
doc.Nodes.Add(new MeshNode { Name = "Box", Primitive = MeshPrimitiveKind.Box });
```
