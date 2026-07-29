using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Evaluation;
using Novolis.Cad.Primitives;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Cad.Services;

/// <summary>Creates CAD generators, MeshFromSolid adapters, and mesh modifiers via the command bus.</summary>
public static class CadModelingActions
{
    public static CadEntity? RequireSelection(CadDocumentSession session) => session.SelectedEntity;

    public static Guid AddGroup(CadCommandBus bus, CadDocumentSession session, string? name = null, IReadOnlyList<Guid>? memberIds = null)
    {
        var id = Guid.NewGuid();
        var entity = new CadEntity
        {
            Id = id,
            Name = name ?? "Group",
            Kind = "group",
            MemberIds = memberIds?.ToList(),
        };
        if (memberIds is not null)
        {
            foreach (var mid in memberIds)
            {
                var child = session.Document.Entities.FirstOrDefault(e => e.Id == mid);
                if (child is not null)
                    child.ParentId = id;
            }
        }

        bus.Execute(new AddEntityCommand(entity));
        return id;
    }

    public static Guid AddBoolean(CadCommandBus bus, Guid targetId, Guid cutterId, string operation = "subtract")
    {
        var entity = new CadEntity
        {
            Kind = "boolean",
            Name = $"Boolean {operation}",
            Operation = operation,
            LeftId = targetId,
            RightId = cutterId,
            TargetId = targetId,
            CutterId = cutterId,
            Mode = "solid",
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddSymmetry(CadCommandBus bus, Guid sourceId, float[]? planePoint = null, float[]? normal = null, bool merge = true)
    {
        var entity = new CadEntity
        {
            Kind = "symmetry",
            Name = "Symmetry",
            SourceId = sourceId,
            PlanePoint = planePoint ?? [0f, 0f, 0f],
            Normal = normal ?? [1f, 0f, 0f],
            MergeAtPlane = merge,
            MergeTolerance = 1e-4f,
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddClone(
        CadCommandBus bus,
        Guid sourceId,
        int[] counts,
        float[] spacing,
        string realization = "instances")
    {
        var entity = new CadEntity
        {
            Kind = "arrayInstance",
            Name = "Cloner",
            PrototypeId = sourceId,
            SourceId = sourceId,
            Counts = counts,
            Spacing = spacing,
            Realization = realization,
            BaseTransform = new CadTransform(),
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddConnect(
        CadCommandBus bus,
        CadDocumentSession session,
        IReadOnlyList<Guid> memberIds,
        string mode = "group")
    {
        if (mode.Equals("group", StringComparison.OrdinalIgnoreCase))
            return AddGroup(bus, session, "Connect Group", memberIds);

        var entity = new CadEntity
        {
            Kind = "connect",
            Name = $"Connect ({mode})",
            MemberIds = memberIds.ToList(),
            Mode = mode,
            TouchEpsilonMeters = 1e-4f,
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddSplit(CadCommandBus bus, Guid sourceId, float[]? planePoint = null, float[]? normal = null)
    {
        var entity = new CadEntity
        {
            Kind = "split",
            Name = "Split",
            SourceId = sourceId,
            Mode = "cuttingPlane",
            PlanePoint = planePoint ?? [0f, 0f, 0f],
            Normal = normal ?? [1f, 0f, 0f],
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddMeshFromSolid(CadCommandBus bus, Guid sourceId, string linkMode = "linked")
    {
        var entity = new CadEntity
        {
            Kind = "meshFromSolid",
            Name = "Mesh From Solid",
            SourceId = sourceId,
            LinkMode = linkMode,
            ParentId = sourceId, // visually under source; parent can be reassigned
        };
        // Prefer sibling under same parent — leave ParentId null and let UI nest
        entity.ParentId = null;
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddWeld(CadCommandBus bus, Guid inputId, float tolerance = 1e-4f)
    {
        var entity = new CadEntity
        {
            Kind = "weld",
            Name = "Weld",
            InputId = inputId,
            SourceId = inputId,
            TouchEpsilonMeters = tolerance,
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddOptimize(CadCommandBus bus, Guid inputId)
    {
        var entity = new CadEntity
        {
            Kind = "optimize",
            Name = "Optimize",
            InputId = inputId,
            SourceId = inputId,
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddBridge(CadCommandBus bus, Guid inputId)
    {
        var entity = new CadEntity
        {
            Kind = "bridge",
            Name = "Bridge",
            InputId = inputId,
            SourceId = inputId,
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddMaterial(CadCommandBus bus, Guid? parentId, string? name = null)
    {
        var entity = new CadEntity
        {
            Kind = "material",
            Name = name ?? "Material",
            ParentId = parentId,
            Color = [0.7f, 0.7f, 0.75f],
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddLight(CadCommandBus bus, Guid? parentId = null)
    {
        var entity = new CadEntity
        {
            Kind = "light",
            Name = "Light",
            ParentId = parentId,
            Center = [0f, 2f, 0f],
            Intensity = 1f,
            LightType = "point",
            Color = [1f, 0.95f, 0.85f],
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    public static Guid AddCamera(CadCommandBus bus, Guid? parentId = null)
    {
        var entity = new CadEntity
        {
            Kind = "camera",
            Name = "Camera",
            ParentId = parentId,
            Center = [4f, 3f, 8f],
            Normal = [0f, 0f, -1f],
        };
        bus.Execute(new AddEntityCommand(entity));
        return entity.Id;
    }

    /// <summary>Apply bridge using equal-count boundary loops on an evaluated mesh.</summary>
    public static EditableMesh? TryBridgeEqualLoops(EditableMesh mesh, BridgeOptions? options = null)
    {
        var loops = mesh.FindBoundaryLoops();
        if (loops.Count < 2)
            return null;
        var a = loops[0];
        var b = loops.Skip(1).FirstOrDefault(l => l.Count == a.Count);
        if (b is null)
            return null;
        return MeshBridge.Apply(mesh, a, b, options);
    }
}
