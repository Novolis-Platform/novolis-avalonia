<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-avalonia">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.3D.Modeling

Avalonia-free evaluated mesh operations for ship/CAD pipelines. Thin façade over `Novolis.Math.Geometry` (`MeshBoolean`, weld, plane split) so domain packages do not reimplement mesh algorithms.

```text
CadDocument → Cad.Evaluation → Novolis.3D.Modeling → Mesh → Novolis.3D.Scene
```

## Install

```bash
dotnet add package Novolis.3D.Modeling
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`). References `Novolis.Math.Geometry`.

## Quick start

```csharp
using Novolis.Math.Geometry;
using Novolis._3D.Modeling;

var cut = ModelingMesh.BooleanDifference(hostMesh, cutterMesh);
var combined = ModelingMesh.Combine([a, b, c]);
```

## Related

| Package / app | Notes |
|---------------|-------|
| [`Novolis.3D.Scene`](../Novolis.3D.Scene/README.md) | Scene graph + `.nov3djson` |
| [`Novolis.Math.Geometry`](https://github.com/Novolis-Platform/novolis-math) | Mesh algorithms |
| [`Novolis.Avalonia.Ship.Design`](../Novolis.Avalonia.Ship.Design/README.md) | Ship eval pipeline consumer |
