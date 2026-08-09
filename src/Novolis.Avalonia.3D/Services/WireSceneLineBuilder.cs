using System.Numerics;
using Novolis.Avalonia._3D.Session;
using Novolis._3D;

namespace Novolis.Avalonia._3D.Services;

/// <summary>World-space line for CAD wire presenters (mesh edges, grid, light/camera gizmos).</summary>
public readonly record struct WireSegment(Vector3 A, Vector3 B, byte R, byte G, byte Blue);

/// <summary>Builds a consistent wireframe line set for OpenGL / CPU / Vulkan presenters.</summary>
public static class WireSceneLineBuilder
{
    /// <summary>Clears and fills <paramref name="dst"/> with grid, mesh edges, light and camera gizmos.</summary>
    public static void Build(SceneSessionService session, List<WireSegment> dst, int gridHalf = 16)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(dst);
        dst.Clear();

        for (var i = -gridHalf; i <= gridHalf; i++)
        {
            Add(dst, new Vector3(i, 0, -gridHalf), new Vector3(i, 0, gridHalf), 55, 75, 95);
            Add(dst, new Vector3(-gridHalf, 0, i), new Vector3(gridHalf, 0, i), 55, 75, 95);
        }

        foreach (var mesh in session.Evaluator.Cache.EvaluatedMeshes)
        {
            var selected = session.Document.SelectionId == mesh.SourceId;
            byte r = selected ? (byte)90 : (byte)140;
            byte g = selected ? (byte)220 : (byte)175;
            byte b = selected ? (byte)230 : (byte)195;
            AppendMeshEdges(dst, mesh, r, g, b);
        }

        foreach (var light in session.Evaluator.Cache.Lights)
            AppendLightGizmo(dst, light, session.Document.SelectionId == light.Source.Id);

        foreach (var cam in session.Evaluator.Cache.Cameras)
            AppendCameraGizmo(dst, cam, session.Document.SelectionId == cam.Source.Id);

        AppendSelectionAxes(dst, session);
    }

    private static void AppendSelectionAxes(List<WireSegment> dst, SceneSessionService session)
    {
        if (session.Document.SelectionId is not { } sid)
            return;

        Vector3? origin = null;
        var mesh = session.Evaluator.Cache.EvaluatedMeshes.FirstOrDefault(m => m.SourceId == sid);
        if (mesh is not null && mesh.Vertices.Length > 0)
        {
            var sum = Vector3.Zero;
            foreach (var v in mesh.Vertices)
                sum += Vector3.Transform(v, mesh.World);
            origin = sum / mesh.Vertices.Length;
        }
        else if (session.Document.Find(sid) is { } node)
        {
            origin = node.Transform.PositionV;
        }

        if (origin is null)
            return;

        var o = origin.Value;
        const float len = 1.2f;
        Add(dst, o, o + Vector3.UnitX * len, 220, 70, 70);
        Add(dst, o, o + Vector3.UnitY * len, 70, 200, 90);
        Add(dst, o, o + Vector3.UnitZ * len, 70, 120, 220);
    }

    private static void AppendMeshEdges(List<WireSegment> dst, EvaluatedMesh mesh, byte r, byte g, byte b)
    {
        var edges = new HashSet<(int, int)>();
        var idx = mesh.Indices;
        for (var t = 0; t + 2 < idx.Length; t += 3)
        {
            AddEdge(edges, idx[t], idx[t + 1]);
            AddEdge(edges, idx[t + 1], idx[t + 2]);
            AddEdge(edges, idx[t + 2], idx[t]);
        }

        var world = mesh.World;
        foreach (var (ia, ib) in edges)
        {
            var pa = Vector3.Transform(mesh.Vertices[ia], world);
            var pb = Vector3.Transform(mesh.Vertices[ib], world);
            Add(dst, pa, pb, r, g, b);
        }
    }

    private static void AppendLightGizmo(List<WireSegment> dst, EvaluatedNode ev, bool selected)
    {
        if (ev.Source is not LightNode light)
            return;

        var p = ev.WorldPosition;
        byte r = selected ? (byte)255 : (byte)System.Math.Clamp((int)(light.Color[0] * 255), 40, 255);
        byte g = selected ? (byte)200 : (byte)System.Math.Clamp((int)(light.Color[1] * 255), 40, 255);
        byte b = selected ? (byte)80 : (byte)System.Math.Clamp((int)(light.Color[2] * 255), 40, 255);
        if (!light.Enabled)
        {
            r = g = b = 80;
        }

        const float s = 0.35f;
        Add(dst, p + new Vector3(-s, 0, 0), p + new Vector3(s, 0, 0), r, g, b);
        Add(dst, p + new Vector3(0, -s, 0), p + new Vector3(0, s, 0), r, g, b);
        Add(dst, p + new Vector3(0, 0, -s), p + new Vector3(0, 0, s), r, g, b);
        Add(dst, p, p + new Vector3(0, -1.2f, 0), r, g, b);
    }

    private static void AppendCameraGizmo(List<WireSegment> dst, EvaluatedNode ev, bool selected)
    {
        var p = ev.WorldPosition;
        byte r = selected ? (byte)255 : (byte)180;
        byte g = selected ? (byte)160 : (byte)180;
        byte b = selected ? (byte)60 : (byte)200;
        const float hx = 0.18f, hy = 0.12f, hz = 0.22f;
        var c000 = p + new Vector3(-hx, -hy, -hz);
        var c001 = p + new Vector3(-hx, -hy, hz);
        var c010 = p + new Vector3(-hx, hy, -hz);
        var c011 = p + new Vector3(-hx, hy, hz);
        var c100 = p + new Vector3(hx, -hy, -hz);
        var c101 = p + new Vector3(hx, -hy, hz);
        var c110 = p + new Vector3(hx, hy, -hz);
        var c111 = p + new Vector3(hx, hy, hz);
        Add(dst, c000, c001, r, g, b); Add(dst, c001, c011, r, g, b); Add(dst, c011, c010, r, g, b); Add(dst, c010, c000, r, g, b);
        Add(dst, c100, c101, r, g, b); Add(dst, c101, c111, r, g, b); Add(dst, c111, c110, r, g, b); Add(dst, c110, c100, r, g, b);
        Add(dst, c000, c100, r, g, b); Add(dst, c001, c101, r, g, b); Add(dst, c010, c110, r, g, b); Add(dst, c011, c111, r, g, b);
    }

    private static void Add(List<WireSegment> dst, Vector3 a, Vector3 b, byte r, byte g, byte bl) =>
        dst.Add(new WireSegment(a, b, r, g, bl));

    private static void AddEdge(HashSet<(int, int)> set, int a, int b)
    {
        if (a == b) return;
        set.Add(a < b ? (a, b) : (b, a));
    }
}
