using System.Drawing;
using System.Numerics;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Evaluation;
using Novolis.Cad.Primitives;
using Novolis.Avalonia.Raylib;
using Novolis.Math.Geometry;
using Novolis.Raylib.Colors;
using Novolis.Raylib.Rendering;
using Novolis.Rendering.Presentation.Silk;

namespace Novolis.Avalonia.Cad.Services;

public sealed class CadModelRenderer
{
    private static readonly Color Background = Color.FromArgb(255, 28, 28, 32);
    private static readonly Color GridMajor = Color.FromArgb(255, 48, 52, 58);
    private static readonly Color GridMinor = Color.FromArgb(255, 36, 38, 42);
    private static readonly Color Sketch = Color.FromArgb(255, 180, 200, 230);
    private static readonly Color Solid = Color.FromArgb(255, 120, 150, 170);
    private static readonly Color Selected = Color.FromArgb(255, 255, 200, 80);
    private static readonly Color Hud = Color.FromArgb(255, 180, 180, 190);

    private readonly CadDocumentSession _session;
    private readonly CadEditorSettings _settings;
    private readonly SilkOrbitCamera _orbit = new()
    {
        Target = new Vector3(0f, 0.5f, 0f),
        Distance = 24f,
        MinDistance = 2f,
        MaxDistance = 400f,
        Yaw = 0.9f,
        Pitch = 0.45f,
    };

    public CadModelEvaluator? Evaluator { get; set; }

    public CadWorkspace Workspace { get; set; } = CadWorkspace.Preview;

    public CadModelRenderer(CadDocumentSession session, CadEditorSettings? settings = null)
    {
        _session = session;
        _settings = settings ?? new CadEditorSettings();
    }

    public SilkOrbitCamera Orbit => _orbit;

    public void Bind(RaylibHostControl host) =>
        host.FrameRendering += (_, e) => OnFrame(e.DeltaSeconds, e.ScreenWidth, e.ScreenHeight);

    public void Fit()
    {
        var (center, radius) = EntityBounds.Compute(_session.Document);
        _orbit.Target = center + new Vector3(0, System.Math.Max(0.5f, radius * 0.15f), 0);
        _orbit.Distance = System.Math.Clamp(System.Math.Max(6f, radius * 2.8f), _orbit.MinDistance, _orbit.MaxDistance);
    }

    public void ApplyDocumentCamera()
    {
        var cam = _session.Document.Camera;
        if (cam.Target is { Length: >= 3 })
            _orbit.Target = CadVec.To(cam.Target);
        if (cam.Distance > 1f)
            _orbit.Distance = System.Math.Clamp(cam.Distance, _orbit.MinDistance, _orbit.MaxDistance);
        _orbit.Yaw = cam.Yaw;
        _orbit.Pitch = cam.Pitch;
    }

    /// <summary>Near-orthographic plan look for ship / floorplate review in Model view.</summary>
    public void ApplyTopDown()
    {
        var (center, radius) = EntityBounds.Compute(_session.Document);
        _orbit.Target = new Vector3(center.X, _settings.Settings.DrawElevation, center.Z);
        _orbit.Distance = System.Math.Clamp(System.Math.Max(20f, radius * 2.4f), _orbit.MinDistance, _orbit.MaxDistance);
        _orbit.Yaw = 0f;
        _orbit.Pitch = 1.35f;
    }

    public void OrbitDrag(float dx, float dy) =>
        _orbit.AddLookDelta(dx * 0.01f, dy * 0.01f);

    public void Zoom(float delta) =>
        _orbit.AdjustDistance(delta > 0 ? -1.5f : 1.5f);

    private void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
    {
        _ = deltaSeconds;
        _ = screenWidth;
        _ = screenHeight;
        Graphics.ClearBackground(Background);
        var eye = _orbit.BuildEyePosition();
        var camera = Camera.Perspective(eye, _orbit.Target, Vector3.UnitY, _orbit.FieldOfViewDegrees);
        World.Begin(camera);
        var elev = _settings.Settings.DrawElevation;
        var gridExtent = EstimateGridExtent();
        World.DrawGrid((int)System.Math.Clamp(gridExtent, 16, 128), 1f);
        DrawGrid(gridExtent);

        var shipExterior = CadShipExterior.ShouldUseExterior(_session.Document)
                           && !_settings.Settings.IsolateLevel;
        if (shipExterior)
        {
            CadShipExterior.Draw(_session.Document);
        }
        else
        {
            DrawDraftingPlane(elev, gridExtent);
            CadEvaluationCache? cache = null;
            if (Evaluator is not null)
            {
                Evaluator.Invalidate(CadEvalStage.Cad);
                cache = Evaluator.Evaluate(_session.Document);
                DrawEvaluated(cache);
            }

            foreach (var entity in _session.Document.Entities)
            {
                if (ShouldSkip(entity))
                    continue;
                // Avoid double-drawing solids already in eval cache
                if (cache is not null && cache.ModeledMeshes.ContainsKey(entity.Id))
                    continue;
                if (cache is not null && cache.CadMeshes.ContainsKey(entity.Id))
                    continue;
                DrawEntity(entity, entity.Id == _session.SelectedId);
            }
        }

        World.End();
        var deck = CadVec.DeckFromElevation(elev);
        var label = Workspace switch
        {
            CadWorkspace.Modeling => "Modeling",
            CadWorkspace.Cad => "CAD",
            _ => "Preview",
        };
        if (shipExterior)
        {
            Graphics.DrawText(
                $"{label} — {_session.Document.Name} · transport exterior",
                8,
                8,
                14,
                Hud);
            Graphics.DrawText(
                "Isolate ON = deck CAD · Isolate OFF = sealed freighter · MMB orbit",
                8,
                26,
                12,
                Hud);
        }
        else
        {
            Graphics.DrawText(
                $"{label} — {_session.Document.Name} · {_session.Document.Entities.Count} ents · draw Y={elev:0.##} (deck {deck})",
                8,
                8,
                14,
                Hud);
            Graphics.DrawText(
                "LMB draw/select · MMB/RMB/Alt+LMB orbit · wheel zoom",
                8,
                26,
                12,
                Hud);
        }
    }

    private bool ShouldSkip(CadEntity entity)
    {
        var kind = entity.Kind.ToLowerInvariant();
        // Generators / adapters are evaluated into meshes; skip raw op entities when we have cache
        if (kind is "boolean" or "weld" or "optimize" or "bridge" or "arrayinstance" or "instance"
            or "symmetry" or "clone" or "connect" or "split" or "meshfromsolid" or "group"
            or "material" or "light" or "camera")
            return true;

        return _settings.Settings.IsolateLevel
               && !CadVec.MatchesLevel(entity, _settings.Settings.DrawElevation, _settings.Settings.LevelTolerance);
    }

    private void DrawEvaluated(CadEvaluationCache cache)
    {
        foreach (var (id, mesh) in cache.ModeledMeshes)
        {
            var selected = id == _session.SelectedId;
            DrawEditableMesh(mesh, selected ? Selected : Solid);
        }

        foreach (var inst in cache.Instances)
        {
            if (inst.Mesh is null)
                continue;
            var copy = inst.Mesh.Clone();
            copy.Transform(inst.Transform);
            DrawEditableMesh(copy, Solid);
        }

        if (Workspace == CadWorkspace.Preview)
        {
            foreach (var light in cache.Lights)
            {
                if (light.Center is null)
                    continue;
                var c = CadVec.To(light.Center);
                World.DrawSphere(c, 0.15f, Color.FromArgb(255, 255, 220, 120));
            }

            foreach (var cam in cache.Cameras)
            {
                if (cam.Center is null)
                    continue;
                var c = CadVec.To(cam.Center);
                World.DrawCube(c, 0.2f, 0.2f, 0.35f, Color.FromArgb(255, 120, 200, 255));
            }
        }
    }

    private static void DrawEditableMesh(EditableMesh mesh, Color color)
    {
        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var a = mesh.Vertices[mesh.Indices[i]];
            var b = mesh.Vertices[mesh.Indices[i + 1]];
            var c = mesh.Vertices[mesh.Indices[i + 2]];
            World.DrawLine(a, b, color);
            World.DrawLine(b, c, color);
            World.DrawLine(c, a, color);
        }
    }

    private static void DrawDraftingPlane(float elev, float extent)
    {
        var y = elev + 0.02f;
        var c = Color.FromArgb(60, 90, 140, 180);
        World.DrawLine(new Vector3(-extent, y, -extent), new Vector3(extent, y, -extent), c);
        World.DrawLine(new Vector3(extent, y, -extent), new Vector3(extent, y, extent), c);
        World.DrawLine(new Vector3(extent, y, extent), new Vector3(-extent, y, extent), c);
        World.DrawLine(new Vector3(-extent, y, extent), new Vector3(-extent, y, -extent), c);
        World.DrawLine(new Vector3(-2, y, 0), new Vector3(2, y, 0), Color.FromArgb(120, 200, 220, 255));
        World.DrawLine(new Vector3(0, y, -2), new Vector3(0, y, 2), Color.FromArgb(120, 200, 220, 255));
    }

    private float EstimateGridExtent()
    {
        var (_, radius) = EntityBounds.Compute(_session.Document);
        return System.Math.Clamp(radius * 1.2f, 20f, 120f);
    }

    private static void DrawGrid(float extent)
    {
        const float step = 1f;
        for (float o = -extent; o <= extent; o += step)
        {
            var major = System.Math.Abs(o) < 0.01f || System.Math.Abs(o % 5f) < 0.01f;
            var c = major ? GridMajor : GridMinor;
            World.DrawLine(new Vector3(-extent, 0.01f, o), new Vector3(extent, 0.01f, o), c);
            World.DrawLine(new Vector3(o, 0.01f, -extent), new Vector3(o, 0.01f, extent), c);
        }
    }

    private static void DrawEntity(CadEntity entity, bool selected)
    {
        var baseColor = ResolveColor(entity, selected);
        switch (entity.Kind.ToLowerInvariant())
        {
            case "line" when entity.A is not null && entity.B is not null:
                World.DrawLine(CadVec.To(entity.A) + new Vector3(0, 0.02f, 0), CadVec.To(entity.B) + new Vector3(0, 0.02f, 0), baseColor);
                break;
            case "circle" when entity.Center is not null:
            {
                var c = CadVec.To(entity.Center) with { Y = 0.02f };
                const int segments = 48;
                Vector3? prev = null;
                for (var i = 0; i <= segments; i++)
                {
                    var a = i * (MathF.PI * 2f / segments);
                    var p = c + new Vector3(MathF.Cos(a) * entity.Radius, 0, MathF.Sin(a) * entity.Radius);
                    if (prev is { } q)
                        World.DrawLine(q, p, baseColor);
                    prev = p;
                }

                break;
            }
            case "rect" when entity.A is not null && entity.B is not null:
            {
                var a = CadVec.To(entity.A) with { Y = 0.02f };
                var b = CadVec.To(entity.B) with { Y = 0.02f };
                var p0 = a;
                var p1 = new Vector3(b.X, 0.02f, a.Z);
                var p2 = b;
                var p3 = new Vector3(a.X, 0.02f, b.Z);
                World.DrawLine(p0, p1, baseColor);
                World.DrawLine(p1, p2, baseColor);
                World.DrawLine(p2, p3, baseColor);
                World.DrawLine(p3, p0, baseColor);
                break;
            }
            case "spline" when entity.ControlPoints is { Count: >= 2 } && entity.Knots is not null:
            {
                var degree = entity.Degree <= 0 ? 3 : entity.Degree;
                var cps = entity.ControlPoints.Select(p => CadVec.To(p)).ToArray();
                var samples = NurbsCurve.Tessellate(degree, cps, entity.Knots, entity.Weights, 64);
                for (var i = 1; i < samples.Length; i++)
                {
                    var a = samples[i - 1] with { Y = 0.02f };
                    var b = samples[i] with { Y = 0.02f };
                    World.DrawLine(a, b, baseColor);
                }

                break;
            }
            case "box" when CadShipGeometry.TryGetBox(entity, out var boxCenter, out var he):
                World.DrawCube(boxCenter, System.Math.Abs(he.X) * 2f, System.Math.Abs(he.Y) * 2f, System.Math.Abs(he.Z) * 2f, baseColor);
                break;
            case "wall":
                DrawWall(entity, baseColor);
                break;
            case "space" when entity.Points is { Count: >= 3 }:
                DrawSpaceFloor(entity, baseColor);
                break;
            case "opening":
                DrawOpening(entity, selected ? Selected : Color.FromArgb(255, 140, 120, 70));
                break;
            case "cylinder" when entity.Center is not null:
                World.DrawCylinder(
                    CadVec.To(entity.Center) - new Vector3(0, entity.Height * 0.5f, 0),
                    entity.Radius,
                    entity.Radius,
                    entity.Height,
                    24,
                    baseColor);
                break;
            case "sphere" when entity.Center is not null:
                World.DrawSphere(CadVec.To(entity.Center), entity.Radius, baseColor);
                break;
        }
    }

    private static Color ResolveColor(CadEntity entity, bool selected)
    {
        if (selected)
            return Selected;
        if (entity.Color is { Length: >= 3 } rgb)
        {
            return Color.FromArgb(
                255,
                (int)(System.Math.Clamp(rgb[0], 0, 1) * 255),
                (int)(System.Math.Clamp(rgb[1], 0, 1) * 255),
                (int)(System.Math.Clamp(rgb[2], 0, 1) * 255));
        }

        return entity.Kind.ToLowerInvariant() switch
        {
            "wall" when entity.Name?.StartsWith("hull", StringComparison.OrdinalIgnoreCase) == true =>
                Color.FromArgb(255, 95, 102, 112),
            "wall" => Color.FromArgb(255, 125, 132, 140),
            "space" when entity.Flags?.Hollow == true => Color.FromArgb(255, 36, 40, 46),
            "space" => Color.FromArgb(255, 58, 62, 68),
            "box" => Solid,
            _ => entity.IsSolid ? Solid : Sketch,
        };
    }

    private static float ShipDeckLift(CadEntity entity)
    {
        var kind = entity.Kind.ToLowerInvariant();
        if (kind is "wall" or "space" or "opening")
            return entity.Deck * CadVec.DeckHeightMeters;
        return 0f;
    }

    private static IEnumerable<(Vector3 A, Vector3 B)> WallSegments(CadEntity wall, float lift)
    {
        if (wall.Points is { Count: >= 2 } pts)
        {
            for (var i = 0; i < pts.Count - 1; i++)
            {
                yield return (
                    CadVec.To(pts[i]) + new Vector3(0, lift, 0),
                    CadVec.To(pts[i + 1]) + new Vector3(0, lift, 0));
            }

            yield break;
        }

        if (wall.A is not null && wall.B is not null)
        {
            yield return (
                CadVec.To(wall.A) + new Vector3(0, lift, 0),
                CadVec.To(wall.B) + new Vector3(0, lift, 0));
        }
    }

    private static void DrawWall(CadEntity wall, Color color)
    {
        var lift = ShipDeckLift(wall);
        var h = System.Math.Max(0.5f, wall.Height);
        var thickness = System.Math.Max(0.08f, wall.Thickness <= 0 ? 0.15f : wall.Thickness);

        foreach (var (a, b) in WallSegments(wall, lift))
        {
            var dir = b - a;
            dir.Y = 0;
            var length = dir.Length();
            if (length < 1e-4f)
                continue;
            dir /= length;
            var mid = (a + b) * 0.5f + new Vector3(0, h * 0.5f, 0);
            DrawWallSlab(mid, dir, length, h, thickness, color);
        }
    }

    /// <summary>Wall slab: true AABB for cardinal runs; post-strip for diagonals.</summary>
    private static void DrawWallSlab(Vector3 center, Vector3 dir, float length, float height, float thickness, Color color)
    {
        if (MathF.Abs(dir.X) < 0.15f || MathF.Abs(dir.Z) < 0.15f)
        {
            var sx = MathF.Abs(dir.X) >= MathF.Abs(dir.Z) ? length : thickness;
            var sz = MathF.Abs(dir.Z) > MathF.Abs(dir.X) ? length : thickness;
            World.DrawCube(center, sx, height, sz, color);
            return;
        }

        var a = center - dir * (length * 0.5f) - new Vector3(0, height * 0.5f, 0);
        var b = center + dir * (length * 0.5f) - new Vector3(0, height * 0.5f, 0);
        World.DrawLine(a, b, color);
        World.DrawLine(a + new Vector3(0, height, 0), b + new Vector3(0, height, 0), color);
        const float step = 0.55f;
        for (var t = 0f; t <= 1.001f; t += step / System.Math.Max(0.55f, length))
        {
            var p = Vector3.Lerp(a, b, t);
            World.DrawCube(p + new Vector3(0, height * 0.5f, 0), thickness, height, thickness, color);
        }
    }

    /// <summary>Floor plate only — walls are separate entities (do not re-extrude space rings).</summary>
    private static void DrawSpaceFloor(CadEntity space, Color color)
    {
        var lift = ShipDeckLift(space);
        var ring = space.Points!.Select(p => CadVec.To(p) + new Vector3(0, lift, 0)).ToArray();
        var min = ring[0];
        var max = ring[0];
        foreach (var p in ring)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        var floorY = min.Y;
        World.DrawCube(
            new Vector3((min.X + max.X) * 0.5f, floorY + 0.03f, (min.Z + max.Z) * 0.5f),
            System.Math.Max(0.2f, max.X - min.X),
            0.06f,
            System.Math.Max(0.2f, max.Z - min.Z),
            color);

        // Light footprint outline (plan cue), not vertical bulkheads.
        for (var i = 0; i < ring.Length; i++)
        {
            var a = ring[i] with { Y = floorY + 0.05f };
            var b = ring[(i + 1) % ring.Length] with { Y = floorY + 0.05f };
            World.DrawLine(a, b, Color.FromArgb(140, Sketch.R, Sketch.G, Sketch.B));
        }
    }

    private static void DrawOpening(CadEntity opening, Color color)
    {
        var lift = ShipDeckLift(opening);
        IReadOnlyList<float[]>? ring = opening.Footprint ?? opening.Points;
        if (ring is { Count: >= 2 })
        {
            for (var i = 0; i < ring.Count; i++)
            {
                var a = CadVec.To(ring[i]) + new Vector3(0, lift + 0.05f, 0);
                var b = CadVec.To(ring[(i + 1) % ring.Count]) + new Vector3(0, lift + 0.05f, 0);
                World.DrawLine(a, b, color);
                World.DrawLine(
                    a + new Vector3(0, System.Math.Max(1f, opening.Height), 0),
                    b + new Vector3(0, System.Math.Max(1f, opening.Height), 0),
                    color);
            }

            return;
        }

        if (opening.A is not null && opening.B is not null)
        {
            var a = CadVec.To(opening.A) + new Vector3(0, lift, 0);
            var b = CadVec.To(opening.B) + new Vector3(0, lift, 0);
            var h = System.Math.Max(1f, opening.Height);
            World.DrawLine(a, b, color);
            World.DrawLine(a + new Vector3(0, h, 0), b + new Vector3(0, h, 0), color);
            World.DrawLine(a, a + new Vector3(0, h, 0), color);
            World.DrawLine(b, b + new Vector3(0, h, 0), color);
        }
    }
}