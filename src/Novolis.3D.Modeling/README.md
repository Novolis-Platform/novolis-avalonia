# Novolis.3D.Modeling

Avalonia-free evaluated mesh operations used by the ship design rendering pipeline:

```text
CadDocument → Cad.Evaluation → Novolis.3D.Modeling → Mesh → Novolis.3D.Scene
```

Algorithms live in `Novolis.Math.Geometry` (`MeshBoolean`, `EditableMesh`, weld/split). This package is the baseline-facing façade so ship/CAD code does not call mesh math ad hoc or reimplement booleans.
