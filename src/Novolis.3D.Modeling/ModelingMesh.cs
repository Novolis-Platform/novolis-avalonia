using Novolis.Math.Geometry;

namespace Novolis._3D.Modeling;

/// <summary>
/// Baseline <c>Novolis.3D.Modeling</c> mesh operations.
/// Implementations delegate to <see cref="Novolis.Math.Geometry"/> — ship/CAD packages must not reimplement mesh algorithms.
/// </summary>
public static class ModelingMesh
{
    public static EditableMesh BooleanUnion(EditableMesh left, EditableMesh right) =>
        MeshBoolean.Apply(left, right, MeshBooleanKind.Union);

    public static EditableMesh BooleanDifference(EditableMesh host, EditableMesh cutter) =>
        MeshBoolean.Apply(host, cutter, MeshBooleanKind.Difference);

    public static EditableMesh BooleanIntersection(EditableMesh left, EditableMesh right) =>
        MeshBoolean.Apply(left, right, MeshBooleanKind.Intersection);

    public static EditableMesh Concat(EditableMesh a, EditableMesh b) =>
        MeshBoolean.Concat(a, b);

    public static EditableMesh Weld(EditableMesh mesh, float tolerance = 1e-4f) =>
        MeshWeld.Apply(mesh, new WeldOptions(tolerance));

    public static PlaneSplitResult Split(EditableMesh mesh, System.Numerics.Plane plane) =>
        MeshPlaneSplit.Split(mesh, plane);

    public static EditableMesh Combine(IEnumerable<EditableMesh> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        EditableMesh? acc = null;
        foreach (var part in parts)
        {
            if (part.VertexCount == 0)
                continue;
            acc = acc is null ? part.Clone() : Concat(acc, part);
        }

        return acc ?? new EditableMesh();
    }
}
