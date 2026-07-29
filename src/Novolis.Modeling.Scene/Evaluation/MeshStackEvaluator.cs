using System.Numerics;

namespace Novolis.Modeling.Scene;

/// <summary>Applies generator/modifier stacks onto mesh nodes (phase 2).</summary>
public static class MeshStackEvaluator
{
    public sealed record DerivedMesh(Guid SourceId, Vector3[] Vertices, int[] Indices, Matrix4x4 World);

    public static IReadOnlyList<DerivedMesh> Expand(SceneDocument document, LookCache cache)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(cache);

        var derived = new List<DerivedMesh>();
        foreach (var gen in document.Nodes.OfType<GeneratorNode>())
        {
            if (gen.SourceId is null || document.Find(gen.SourceId.Value) is not MeshNode source)
                continue;
            var sourceEval = cache.Meshes.FirstOrDefault(m => m.Source.Id == source.Id);
            if (sourceEval is null)
                continue;

            switch (gen.Generator)
            {
                case GeneratorKind.Cloner:
                {
                    var offset = new Vector3(gen.Offset[0], gen.Offset[1], gen.Offset[2]);
                    for (var i = 0; i < System.Math.Max(1, gen.Count); i++)
                    {
                        var t = Matrix4x4.CreateTranslation(offset * i) * sourceEval.WorldMatrix;
                        derived.Add(MakePrimitive(source, t));
                    }

                    break;
                }
                case GeneratorKind.Symmetry:
                {
                    derived.Add(MakePrimitive(source, sourceEval.WorldMatrix));
                    var axis = gen.Axis.ToLowerInvariant();
                    var mirror = axis switch
                    {
                        "y" => Matrix4x4.CreateScale(1, -1, 1),
                        "z" => Matrix4x4.CreateScale(1, 1, -1),
                        _ => Matrix4x4.CreateScale(-1, 1, 1),
                    };
                    derived.Add(MakePrimitive(source, mirror * sourceEval.WorldMatrix));
                    break;
                }
                case GeneratorKind.Extrude:
                    derived.Add(MakePrimitive(source, sourceEval.WorldMatrix));
                    break;
            }
        }

        // Weld/Optimize/Subdivision markers attach to InputId; v1 records presence without topology rewrite.
        foreach (var mod in document.Nodes.OfType<ModifierNode>())
        {
            if (mod.InputId is null)
                continue;
            var mesh = cache.Meshes.FirstOrDefault(m => m.Source.Id == mod.InputId.Value);
            if (mesh?.Source is MeshNode mn)
                derived.Add(MakePrimitive(mn, mesh.WorldMatrix));
        }

        return derived;
    }

    private static DerivedMesh MakePrimitive(MeshNode source, Matrix4x4 world)
    {
        // Lightweight unit box corners as placeholder geometry for generator expansion.
        Vector3[] verts =
        [
            new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f),
            new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f),
        ];
        var sx = source.Size[0];
        var sy = source.Size[1];
        var sz = source.Size[2];
        for (var i = 0; i < verts.Length; i++)
            verts[i] = new Vector3(verts[i].X * sx, verts[i].Y * sy, verts[i].Z * sz);

        int[] indices =
        [
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 4, 7, 6,
            0, 4, 5, 0, 5, 1,
            2, 6, 7, 2, 7, 3,
            0, 3, 7, 0, 7, 4,
            1, 5, 6, 1, 6, 2,
        ];
        return new DerivedMesh(source.Id, verts, indices, world);
    }
}
