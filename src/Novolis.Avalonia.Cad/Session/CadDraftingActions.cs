using System.Globalization;
using System.Text.Json;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;
using Novolis.Cad.SceneBridge;
using Novolis._3D;

namespace Novolis.Avalonia.Cad.Session;

/// <summary>Drafting / appearance / bridge actions shared by UI and agent Execute.</summary>
internal static class CadDraftingActions
{
    public static CadCommandResultDto ExportScene(CadDocumentSession document, CadCommandDto command)
    {
        var path = command.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            var basePath = document.DocumentPath;
            path = string.IsNullOrWhiteSpace(basePath)
                ? Path.Combine(Path.GetTempPath(), $"{Sanitize(document.Document.Name)}.nov3djson")
                : Path.ChangeExtension(basePath, ".nov3djson");
        }

        CadSceneBridge.SaveNov3dJson(document.Document, path!, new CadSceneBridgeOptions
        {
            EnsureStudioLights = PropBool(command, "ensureStudioLights", true),
        });
        return Ok(CadSessionActionIds.ExportScene, path!, [path!]);
    }

    public static (CadCommandResultDto Result, SceneDocument Scene) BridgeScene(
        CadDocumentSession document,
        CadCommandDto command)
    {
        var scene = CadSceneBridge.ToSceneDocument(document.Document, new CadSceneBridgeOptions
        {
            EnsureStudioLights = PropBool(command, "ensureStudioLights", true),
        });
        return (Ok(CadSessionActionIds.BridgeScene, $"Bridged {scene.Nodes.OfType<MeshNode>().Count()} meshes."), scene);
    }

    public static CadCommandResultDto SetMaterial(CadCommandBus bus, CadDocumentSession document, CadCommandDto command)
    {
        var id = command.EntityId ?? document.SelectedId;
        if (id is null)
            return Fail(CadSessionActionIds.SetMaterial, "Select an entity.", "noSelection");
        var entity = document.Document.Entities.FirstOrDefault(e => e.Id == id.Value);
        if (entity is null)
            return Fail(CadSessionActionIds.SetMaterial, "Entity not found.", "badEntity");

        var material = command.Kind
                       ?? Prop(command, "material")
                       ?? Prop(command, "name")
                       ?? "";
        var before = entity.Material;
        bus.Execute(new MutateEntityFieldsCommand(
            entity.Id,
            "Set material",
            e => e.Material = string.IsNullOrWhiteSpace(material) ? null : material,
            e => e.Material = before));
        return Ok(CadSessionActionIds.SetMaterial, $"Material → {material}");
    }

    public static CadCommandResultDto SetWallSide(CadCommandBus bus, CadDocumentSession document, CadCommandDto command)
    {
        var id = command.EntityId ?? document.SelectedId;
        if (id is null)
            return Fail(CadSessionActionIds.SetWallSide, "Select a wall.", "noSelection");
        var entity = document.Document.Entities.FirstOrDefault(e => e.Id == id.Value);
        if (entity is null || !entity.Kind.Equals("wall", StringComparison.OrdinalIgnoreCase))
            return Fail(CadSessionActionIds.SetWallSide, "Wall entity required.", "badEntity");

        var side = (Prop(command, "side") ?? "A").Trim().ToUpperInvariant();
        Guid? shapeId = null;
        if (Guid.TryParse(Prop(command, "shapeId") ?? command.Kind, out var parsed))
            shapeId = parsed;

        var before = entity.Sides is null
            ? null
            : JsonSerializer.Serialize(entity.Sides);
        bus.Execute(new MutateEntityFieldsCommand(
            entity.Id,
            "Set wall side",
            e =>
            {
                e.Sides ??= new CadWallSides();
                if (side is "B")
                    e.Sides.B = new CadWallSide { ShapeId = shapeId };
                else
                    e.Sides.A = new CadWallSide { ShapeId = shapeId };
            },
            e =>
            {
                e.Sides = string.IsNullOrEmpty(before)
                    ? null
                    : JsonSerializer.Deserialize<CadWallSides>(before);
            }));
        return Ok(CadSessionActionIds.SetWallSide, $"Wall side {side} → {shapeId}");
    }

    public static CadCommandResultDto AddWall(CadCommandBus bus, CadCommandDto command)
    {
        var pts = ParsePoints(command);
        CadEntity entity;
        if (pts is { Count: >= 2 })
        {
            entity = new CadEntity
            {
                Name = Prop(command, "name") ?? "Wall",
                Kind = "wall",
                Points = pts,
                Height = PropFloat(command, "height", 2.4f),
                Thickness = PropFloat(command, "thickness", 0.15f),
                Deck = (int)PropFloat(command, "deck", 0f),
            };
        }
        else if (command.Center is { Length: >= 3 } && TryPropVec3(command, "b", out var b))
        {
            entity = new CadEntity
            {
                Name = Prop(command, "name") ?? "Wall",
                Kind = "wall",
                A = command.Center,
                B = b,
                Height = PropFloat(command, "height", 2.4f),
                Thickness = PropFloat(command, "thickness", 0.15f),
                Deck = (int)PropFloat(command, "deck", 0f),
            };
        }
        else if (TryPropVec3(command, "a", out var a) && TryPropVec3(command, "b", out var b2))
        {
            entity = new CadEntity
            {
                Name = Prop(command, "name") ?? "Wall",
                Kind = "wall",
                A = a,
                B = b2,
                Height = PropFloat(command, "height", 2.4f),
                Thickness = PropFloat(command, "thickness", 0.15f),
                Deck = (int)PropFloat(command, "deck", 0f),
            };
        }
        else
        {
            return Fail(CadSessionActionIds.AddWall, "Need points or a/b.", "badArgs");
        }

        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddWall, $"Wall {entity.Id}", [entity.Id.ToString()]);
    }

    public static CadCommandResultDto ExtrudeProfile(CadCommandBus bus, CadCommandDto command)
    {
        var pts = ParsePoints(command);
        if (pts is not { Count: >= 3 })
            return Fail(CadSessionActionIds.ExtrudeProfile, "Closed profile points required (>=3).", "badArgs");

        var height = PropFloat(command, "height", 2.4f);
        // Rect-like AABB → box; otherwise wall loop from polyline.
        if (pts.Count is 4 or 5)
        {
            var minX = pts.Min(p => p[0]);
            var maxX = pts.Max(p => p[0]);
            var minZ = pts.Min(p => p[2]);
            var maxZ = pts.Max(p => p[2]);
            var cx = (minX + maxX) * 0.5f;
            var cz = (minZ + maxZ) * 0.5f;
            var box = new CadEntity
            {
                Name = Prop(command, "name") ?? "Extrude",
                Kind = "box",
                Center = [cx, height * 0.5f, cz],
                HalfExtents = [(maxX - minX) * 0.5f, height * 0.5f, (maxZ - minZ) * 0.5f],
            };
            if (CadVec.LooksLikeShipDocument(bus.Session.Document))
            {
                box.Properties = new Dictionary<string, JsonElement>
                {
                    ["exterior"] = JsonSerializer.SerializeToElement(true),
                };
            }

            bus.Execute(new AddEntityCommand(box));
            return Ok(CadSessionActionIds.ExtrudeProfile, $"Box {box.Id}", [box.Id.ToString()]);
        }

        var wall = new CadEntity
        {
            Name = Prop(command, "name") ?? "Extrude Wall",
            Kind = "wall",
            Points = pts,
            Height = height,
            Thickness = PropFloat(command, "thickness", 0.2f),
        };
        bus.Execute(new AddEntityCommand(wall));
        return Ok(CadSessionActionIds.ExtrudeProfile, $"Wall {wall.Id}", [wall.Id.ToString()]);
    }

    public static CadCommandResultDto AddDimension(CadCommandBus bus, CadCommandDto command)
    {
        if (!TryPropVec3(command, "a", out var a) || !TryPropVec3(command, "b", out var b))
        {
            if (command.Center is { Length: >= 3 } && TryPropVec3(command, "b", out b))
                a = command.Center;
            else
                return Fail(CadSessionActionIds.AddDimension, "Need a and b.", "badArgs");
        }

        var entity = new CadEntity
        {
            Name = Prop(command, "name") ?? "Dimension",
            Kind = "dimension",
            A = a,
            B = b,
            Height = PropFloat(command, "offset", 0.35f),
        };
        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddDimension, $"Dimension {entity.Id}", [entity.Id.ToString()]);
    }

    public static CadCommandResultDto AddLine(CadCommandBus bus, CadCommandDto command)
    {
        if (!TryEndpoints(command, out var a, out var b))
            return Fail(CadSessionActionIds.AddLine, "Need a and b.", "badArgs");
        var entity = new CadEntity
        {
            Name = "Line",
            Kind = "line",
            A = a,
            B = b,
            Style = new CadStyle { Linetype = "Continuous" },
        };
        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddLine, $"Line {entity.Id}", [entity.Id.ToString()]);
    }

    public static CadCommandResultDto AddCircle(CadCommandBus bus, CadCommandDto command)
    {
        var center = command.Center ?? (TryPropVec3(command, "center", out var c) ? c : null);
        if (center is null)
            return Fail(CadSessionActionIds.AddCircle, "Need center.", "badArgs");
        var radius = PropFloat(command, "radius", command.Tolerance ?? 1f);
        var entity = new CadEntity
        {
            Name = "Circle",
            Kind = "circle",
            Center = center,
            Radius = radius,
        };
        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddCircle, $"Circle {entity.Id}", [entity.Id.ToString()]);
    }

    public static CadCommandResultDto AddRect(CadCommandBus bus, CadCommandDto command)
    {
        if (!TryEndpoints(command, out var a, out var b))
            return Fail(CadSessionActionIds.AddRect, "Need a and b corners.", "badArgs");
        var entity = new CadEntity
        {
            Name = "Rect",
            Kind = "rect",
            A = a,
            B = b,
        };
        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddRect, $"Rect {entity.Id}", [entity.Id.ToString()]);
    }

    public static CadCommandResultDto AddSpline(CadCommandBus bus, CadCommandDto command)
    {
        var pts = ParsePoints(command);
        if (pts is not { Count: >= 2 })
            return Fail(CadSessionActionIds.AddSpline, "Need points (>=2).", "badArgs");
        var fit = pts.Select(p => new System.Numerics.Vector3(p[0], p[1], p[2])).ToList();
        var (degree, controls, knots, weights) = Novolis.Math.Geometry.NurbsCurve.FromFitPoints(fit);
        var entity = new CadEntity
        {
            Name = "Spline",
            Kind = "spline",
            Degree = degree,
            ControlPoints = controls.Select(CadVec.From).ToList(),
            Knots = knots,
            Weights = weights,
            FitPoints = pts,
            Closed = PropBool(command, "closed", false),
        };
        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddSpline, $"Spline {entity.Id}", [entity.Id.ToString()]);
    }

    public static CadCommandResultDto AddBox(CadCommandBus bus, CadCommandDto command)
    {
        var center = command.Center ?? [0f, 0.5f, 0f];
        var he = command.Spacing ?? [0.5f, 0.5f, 0.5f];
        if (command.Properties is not null && command.Properties.TryGetValue("halfExtents", out var heRaw))
            he = ParseVec3(heRaw) ?? he;
        var entity = new CadEntity
        {
            Name = Prop(command, "name") ?? "Box",
            Kind = "box",
            Center = center,
            HalfExtents = he,
        };
        // Ship docs: boxes drawn in the app are exterior massing unless marked otherwise.
        if (CadVec.LooksLikeShipDocument(bus.Session.Document))
        {
            entity.Properties = new Dictionary<string, JsonElement>
            {
                ["exterior"] = JsonSerializer.SerializeToElement(true),
            };
        }

        bus.Execute(new AddEntityCommand(entity));
        return Ok(CadSessionActionIds.AddBox, $"Box {entity.Id}", [entity.Id.ToString()]);
    }

    private static bool TryEndpoints(CadCommandDto command, out float[] a, out float[] b)
    {
        a = null!;
        b = null!;
        if (TryPropVec3(command, "a", out a) && TryPropVec3(command, "b", out b))
            return true;
        if (command.Center is { Length: >= 3 } && TryPropVec3(command, "b", out b))
        {
            a = command.Center;
            return true;
        }

        return false;
    }

    private static List<float[]>? ParsePoints(CadCommandDto command)
    {
        var raw = Prop(command, "points");
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var list = new List<float[]>();
        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var v = ParseVec3(part);
            if (v is not null)
                list.Add(v);
        }

        return list.Count >= 2 ? list : null;
    }

    private static float[]? ParseVec3(string raw)
    {
        var bits = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (bits.Length < 3)
            return null;
        if (!float.TryParse(bits[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
            return null;
        if (!float.TryParse(bits[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            return null;
        if (!float.TryParse(bits[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            return null;
        return [x, y, z];
    }

    private static bool TryPropVec3(CadCommandDto command, string key, out float[] vec)
    {
        vec = null!;
        var raw = Prop(command, key);
        if (raw is null)
            return false;
        var parsed = ParseVec3(raw);
        if (parsed is null)
            return false;
        vec = parsed;
        return true;
    }

    private static string? Prop(CadCommandDto command, string key) =>
        command.Properties is not null && command.Properties.TryGetValue(key, out var v) ? v : null;

    private static float PropFloat(CadCommandDto command, string key, float fallback)
    {
        var raw = Prop(command, key);
        return raw is not null
               && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v
            : fallback;
    }

    private static bool PropBool(CadCommandDto command, string key, bool fallback)
    {
        var raw = Prop(command, key);
        if (raw is null)
            return fallback;
        return raw is "1" or "true" or "yes" || (bool.TryParse(raw, out var b) && b);
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "scene" : name;
    }

    private static CadCommandResultDto Ok(string id, string message, string[]? artifacts = null) =>
        new()
        {
            Ok = true,
            ActionId = id,
            Message = message,
            Paths = artifacts,
        };

    private static CadCommandResultDto Fail(string id, string message, string code) =>
        new()
        {
            Ok = false,
            ActionId = id,
            Message = message,
            ErrorCode = code,
        };
}
