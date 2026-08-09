<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.3D.Import

Assimp-based mesh import (FBX, OBJ, glTF, …) into `Novolis.Math.Geometry` `TriangleMesh` / `EditableMesh`. Native Assimp runtime required.

## Install

```bash
dotnet add package Novolis.3D.Import
```

## Quick start

```csharp
using Novolis._3D;

var mesh = AssimpMeshImporter.ImportEditable(@"model.fbx");
```

## Related

| Package | Notes |
|---------|-------|
| [`Novolis.3D.Scene`](../Novolis.3D.Scene/README.md) | Scene graph for imported meshes |
| [`Novolis.Avalonia.3D`](../Novolis.Avalonia.3D/README.md) | Scene editor UI |
