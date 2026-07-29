namespace Novolis.Avalonia.Cad.Scene;

using Novolis.Cad.Primitives;

/// <summary>Projected scene-tree row over a <see cref="CadEntity"/>.</summary>
public sealed record CadSceneTreeNode(
    Guid Id,
    string Name,
    string Kind,
    CadSceneNodeCategory Category,
    Guid? ParentId,
    IReadOnlyList<CadSceneTreeNode> Children,
    string? Role = null);

/// <summary>Builds a hierarchy from <see cref="CadEntity.ParentId"/> and classifies node categories.</summary>
public static class CadSceneGraph
{
    public static IReadOnlyList<CadSceneTreeNode> BuildTree(CadDocument document, CadWorkspace? filter = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var byParent = document.Entities
            .GroupBy(e => e.ParentId)
            .ToDictionary(g => g.Key ?? Guid.Empty, g => g.OrderBy(e => e.Name ?? e.Kind).ToList());

        List<CadEntity> Roots()
        {
            if (byParent.TryGetValue(Guid.Empty, out var roots))
                return roots;
            // Orphans / flat docs: treat all as roots
            return document.Entities.OrderBy(e => e.Name ?? e.Kind).ToList();
        }

        CadSceneTreeNode Build(CadEntity entity)
        {
            byParent.TryGetValue(entity.Id, out var kids);
            kids ??= [];
            var children = kids
                .Select(Build)
                .Where(n => filter is null || IsVisibleInWorkspace(n.Category, n.Kind, filter.Value))
                .ToList();
            var category = Classify(entity);
            return new CadSceneTreeNode(
                entity.Id,
                entity.Name ?? entity.Kind,
                entity.Kind,
                category,
                entity.ParentId,
                children,
                InferRole(entity));
        }

        return Roots()
            .Select(Build)
            .Where(n => filter is null || IsVisibleInWorkspace(n.Category, n.Kind, filter.Value) || n.Children.Count > 0)
            .ToList();
    }

    public static CadSceneNodeCategory Classify(CadEntity entity)
    {
        var kind = entity.Kind.ToLowerInvariant();
        return kind switch
        {
            "group" => CadSceneNodeCategory.Group,
            "boolean" or "symmetry" or "arrayinstance" or "instance" or "clone" or "connect" or "split"
                or "extrude" or "revolve" or "sweep" or "fillet" or "chamfer" => CadSceneNodeCategory.Generator,
            "meshfromsolid" => CadSceneNodeCategory.MeshFromSolid,
            "weld" or "optimize" or "bridge" or "inset" or "faceextrude" or "subdivide" or "smooth"
                or "normals" or "uvproject" => CadSceneNodeCategory.MeshModifier,
            "material" => CadSceneNodeCategory.Material,
            "light" => CadSceneNodeCategory.Light,
            "camera" => CadSceneNodeCategory.Camera,
            "transform" => CadSceneNodeCategory.Transform,
            "box" or "cylinder" or "sphere" or "cone" or "wedge" or "mesh" or "wall" or "space"
                or "line" or "circle" or "rect" or "spline" or "opening" => CadSceneNodeCategory.Geometry,
            _ => CadSceneNodeCategory.Unknown,
        };
    }

    public static bool IsVisibleInWorkspace(CadSceneNodeCategory category, string kind, CadWorkspace workspace)
    {
        _ = kind;
        return workspace switch
        {
            CadWorkspace.Cad =>
                category is CadSceneNodeCategory.Group
                    or CadSceneNodeCategory.Geometry
                    or CadSceneNodeCategory.Generator
                    or CadSceneNodeCategory.Transform,
            CadWorkspace.Modeling => category is CadSceneNodeCategory.Group or CadSceneNodeCategory.Geometry
                or CadSceneNodeCategory.MeshFromSolid or CadSceneNodeCategory.MeshModifier
                or CadSceneNodeCategory.Generator or CadSceneNodeCategory.Transform,
            CadWorkspace.Preview => category is CadSceneNodeCategory.Group or CadSceneNodeCategory.Geometry
                or CadSceneNodeCategory.Material or CadSceneNodeCategory.Light or CadSceneNodeCategory.Camera
                or CadSceneNodeCategory.MeshFromSolid or CadSceneNodeCategory.MeshModifier
                or CadSceneNodeCategory.Transform or CadSceneNodeCategory.Generator,
            _ => true,
        };
    }

    private static string? InferRole(CadEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.OperandRole))
            return entity.OperandRole;
        if (entity.Kind.Equals("boolean", StringComparison.OrdinalIgnoreCase))
            return entity.Operation;
        return null;
    }

    public static IEnumerable<CadEntity> ChildrenOf(CadDocument document, Guid parentId) =>
        document.Entities.Where(e => e.ParentId == parentId);

    public static CadEntity? Find(CadDocument document, Guid id) =>
        document.Entities.FirstOrDefault(e => e.Id == id);
}
