using System.Numerics;
using Novolis.Cad.Primitives;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Cad.Evaluation;

/// <summary>Forwards to <see cref="Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator"/> (library home).</summary>
public static class CadSolidTessellator
{
    public static EditableMesh? TryTessellate(CadEntity entity) =>
        Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator.TryTessellate(entity);

    public static EditableMesh FromStored(CadEntity entity) =>
        Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator.FromStored(entity);

    public static void StoreOnEntity(CadEntity entity, EditableMesh mesh) =>
        Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator.StoreOnEntity(entity, mesh);

    public static EditableMesh Box(Vector3 he) =>
        Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator.Box(he);

    public static EditableMesh Sphere(float radius, int rings, int slices) =>
        Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator.Sphere(radius, rings, slices);

    public static EditableMesh Cylinder(float radius, float height, int slices) =>
        Novolis.Cad.SceneBridge.Tessellation.CadSolidTessellator.Cylinder(radius, height, slices);
}
