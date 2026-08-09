<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.3D.Scene

Mesh-first 3D scene graph for editing and rendering pipelines. Typed nodes, staged evaluation (generators/modifiers → triangles), runtime mesh-edit state, and `.nov3djson` serialization. No Avalonia UI and no LLM transports.

## Install

```bash
dotnet add package Novolis.3D.Scene
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`). References `Novolis.Math.Geometry`.

## Quick start

```csharp
using Novolis._3D;

var doc = SceneDocument.CreatePrimitiveStage("Demo");
SceneSerializer.Save(doc, @"out.nov3djson");
var evaluator = new SceneEvaluator();
evaluator.Bind(doc);
```

## Related

| Package / app | Notes |
|---------------|-------|
| [`Novolis.3D.Import`](../Novolis.3D.Import/README.md) | Assimp import into editable meshes |
| [`Novolis.Avalonia.3D`](../Novolis.Avalonia.3D/README.md) | Scene editor UI |
| [`Novolis.Cad.SceneBridge`](https://github.com/Novolis-Platform/novolis-cad) | CadDocument → SceneDocument |
