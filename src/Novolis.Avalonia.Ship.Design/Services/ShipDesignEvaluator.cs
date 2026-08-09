using Novolis.Cad.Evaluation;
using Novolis.Cad.Primitives;
using Novolis.Math.Geometry;
using Novolis.Ship.Design;
using Novolis._3D;
using Novolis._3D.Modeling;

namespace Novolis.Avalonia.Ship.Design.Services;

public sealed class ShipDesignEvaluationResult
{
    public required int ObjectCount { get; init; }
    public required int CutoutCount { get; init; }
    public required int MeshNodeCount { get; init; }
    public required CadDocument FlatCad { get; init; }
    public required SceneDocument Scene { get; init; }
    public required string? ScenePath { get; init; }
}

/// <summary>
/// Baseline rendering pipeline (§22):
/// ShipDesign → per-object CadDocument → Cad.Evaluation → 3D.Modeling cutouts → 3D.Scene.
/// Neither Ship.Design nor Cad packages render; this composes evaluated meshes only.
/// </summary>
public static class ShipDesignEvaluator
{
    public static ShipDesignEvaluationResult Evaluate(ShipDesign design, string? scenePath = null)
    {
        ArgumentNullException.ThrowIfNull(design);

        var hostMeshes = new Dictionary<Guid, EditableMesh>();
        var labels = new Dictionary<Guid, string>();

        foreach (var (id, geom, kind) in design.GeometricObjects())
        {
            var mesh = EvaluateObjectMesh(geom);
            if (mesh.VertexCount == 0)
                continue;
            hostMeshes[id.Value] = mesh;
            labels[id.Value] = $"{kind}:{geom.Name}";
        }

        // Apply structural cutouts as mesh differences (relationship → derived geometry).
        foreach (var cutout in design.Cutouts)
        {
            if (!hostMeshes.TryGetValue(cutout.HostId.Value, out var host))
                continue;
            if (!hostMeshes.TryGetValue(cutout.SourceId.Value, out var source))
                continue;
            hostMeshes[cutout.HostId.Value] = ModelingMesh.BooleanDifference(host, source);
        }

        var scene = ComposeScene(design.Ship.Name, hostMeshes, labels);
        if (!string.IsNullOrWhiteSpace(scenePath))
            SceneSerializer.Save(scene, scenePath);

        return new ShipDesignEvaluationResult
        {
            ObjectCount = design.GeometricObjects().Count(),
            CutoutCount = design.Cutouts.Count,
            MeshNodeCount = scene.Nodes.OfType<MeshNode>().Count(),
            FlatCad = ShipCadProjector.ToCadDocument(design),
            Scene = scene,
            ScenePath = scenePath,
        };
    }

    private static EditableMesh EvaluateObjectMesh(CadDocument geometry)
    {
        var evaluator = new CadModelEvaluator();
        var cache = evaluator.Evaluate(geometry);
        var parts = new List<EditableMesh>();
        foreach (var mesh in cache.ModeledMeshes.Values)
        {
            if (mesh.VertexCount > 0)
                parts.Add(mesh);
        }

        if (parts.Count == 0)
        {
            foreach (var mesh in cache.CadMeshes.Values)
            {
                if (mesh.VertexCount > 0)
                    parts.Add(mesh);
            }
        }

        if (parts.Count == 0)
        {
            foreach (var inst in cache.Instances)
            {
                if (inst.Mesh is null || inst.Mesh.VertexCount == 0)
                    continue;
                var copy = inst.Mesh.Clone();
                copy.Transform(inst.Transform);
                parts.Add(copy);
            }
        }

        return ModelingMesh.Combine(parts);
    }

    private static SceneDocument ComposeScene(
        string shipName,
        IReadOnlyDictionary<Guid, EditableMesh> meshes,
        IReadOnlyDictionary<Guid, string> labels)
    {
        var scene = new SceneDocument
        {
            Name = shipName,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            Generator = "Novolis.Avalonia.Ship.Design",
        };
        var root = new GroupNode { Name = "Ship" };
        scene.Nodes.Add(root);

        foreach (var (id, mesh) in meshes)
        {
            if (mesh.VertexCount == 0)
                continue;
            var node = new MeshNode
            {
                Name = labels.TryGetValue(id, out var label) ? label : id.ToString("N"),
                ParentId = root.Id,
                Primitive = MeshPrimitiveKind.Box,
            };
            MeshEditBake.WriteBaked(node, mesh);
            scene.Nodes.Add(node);
        }

        return scene;
    }
}
