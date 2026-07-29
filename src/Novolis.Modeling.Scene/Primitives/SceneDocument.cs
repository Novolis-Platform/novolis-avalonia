namespace Novolis.Modeling.Scene;

/// <summary>Authoring document persisted as <c>.nov3djson</c>.</summary>
public sealed class SceneDocument
{
    public string Format { get; set; } = "novolis.scene";
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Untitled";
    public string? Generator { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public float UnitScaleMeters { get; set; } = 1f;
    public List<SceneNode> Nodes { get; set; } = [];
    public Guid? ActiveCameraId { get; set; }
    public Guid? SelectionId { get; set; }
    public Dictionary<string, string>? Properties { get; set; }

    public SceneNode? Find(Guid id) => Nodes.FirstOrDefault(n => n.Id == id);

    public IEnumerable<SceneNode> ChildrenOf(Guid? parentId) =>
        Nodes.Where(n => n.ParentId == parentId);

    public IReadOnlyList<SceneNode> Roots() => ChildrenOf(null).ToList();

    public bool TryRemove(Guid id)
    {
        var node = Find(id);
        if (node is null)
            return false;
        Nodes.Remove(node);
        foreach (var child in Nodes.Where(n => n.ParentId == id).ToList())
            child.ParentId = node.ParentId;
        if (SelectionId == id)
            SelectionId = null;
        if (ActiveCameraId == id)
            ActiveCameraId = null;
        return true;
    }

    public static SceneDocument CreateEmpty(string name = "Untitled")
    {
        var doc = new SceneDocument
        {
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        var root = new GroupNode { Name = "Scene" };
        var cam = new CameraNode
        {
            Name = "Camera",
            ParentId = root.Id,
            Transform = new SceneTransform { Position = [4, 3, 6] },
            Target = [0, 0.5f, 0],
        };
        var key = new LightNode
        {
            Name = "Key Light",
            ParentId = root.Id,
            LightKind = LightKind.Omni,
            Intensity = 2.5f,
            Transform = new SceneTransform { Position = [2, 4, 2] },
        };
        var fill = new LightNode
        {
            Name = "Fill",
            ParentId = root.Id,
            LightKind = LightKind.Area,
            Intensity = 0.6f,
            Color = [0.85f, 0.9f, 1f],
            AreaSize = [2, 1],
            Transform = new SceneTransform { Position = [-3, 2, 1] },
        };
        var floor = new MeshNode
        {
            Name = "Floor",
            ParentId = root.Id,
            Primitive = MeshPrimitiveKind.Plane,
            Size = [8, 0.05f, 8],
            Transform = new SceneTransform { Position = [0, 0, 0] },
        };
        var box = new MeshNode
        {
            Name = "Box",
            ParentId = root.Id,
            Primitive = MeshPrimitiveKind.Box,
            Size = [1, 1, 1],
            Transform = new SceneTransform { Position = [0, 0.5f, 0] },
        };
        doc.Nodes.AddRange([root, cam, key, fill, floor, box]);
        doc.ActiveCameraId = cam.Id;
        return doc;
    }

    public static SceneDocument CreateSpotRimSample()
    {
        var doc = CreateEmpty("Spot Rim Stage");
        doc.Nodes.RemoveAll(n => n is LightNode);
        var root = doc.Roots().OfType<GroupNode>().First();
        doc.Nodes.Add(new LightNode
        {
            Name = "Rim Spot",
            ParentId = root.Id,
            LightKind = LightKind.Spot,
            Intensity = 4f,
            ConeAngleDeg = 28f,
            PenumbraDeg = 8f,
            Color = [1f, 0.95f, 0.85f],
            Transform = new SceneTransform
            {
                Position = [0, 3, -3],
                RotationDeg = [35, 0, 0],
            },
        });
        doc.Nodes.Add(new LightNode
        {
            Name = "Sun",
            ParentId = root.Id,
            LightKind = LightKind.Infinite,
            Intensity = 0.35f,
            Color = [0.7f, 0.8f, 1f],
            Transform = new SceneTransform { RotationDeg = [-50, 30, 0] },
        });
        doc.ModifiedAt = DateTimeOffset.UtcNow;
        return doc;
    }

    public static SceneDocument CreateMultiLightStudio()
    {
        var doc = CreateEmpty("Multi-Light Studio");
        var root = doc.Roots().OfType<GroupNode>().First();
        doc.Nodes.Add(new LightNode
        {
            Name = "Back Spot",
            ParentId = root.Id,
            LightKind = LightKind.Spot,
            Intensity = 2.2f,
            ConeAngleDeg = 40f,
            Transform = new SceneTransform { Position = [0, 2.5f, -4], RotationDeg = [20, 180, 0] },
        });
        doc.Nodes.Add(new MeshNode
        {
            Name = "Sphere",
            ParentId = root.Id,
            Primitive = MeshPrimitiveKind.Sphere,
            Size = [0.8f, 0.8f, 0.8f],
            Transform = new SceneTransform { Position = [1.5f, 0.8f, 0] },
        });
        doc.ModifiedAt = DateTimeOffset.UtcNow;
        return doc;
    }
}
