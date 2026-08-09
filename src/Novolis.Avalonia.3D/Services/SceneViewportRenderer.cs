using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Novolis.Avalonia.Raylib;
using Novolis.Avalonia._3D.Session;
using Novolis._3D;
using Novolis.Raylib.Rendering;
using Novolis.Simulation.View;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Draws evaluated triangle meshes + light/camera gizmos + component highlights.</summary>
public sealed class SceneViewportRenderer
{
    private static readonly Color Background = Color.FromArgb(255, 18, 24, 32);
    private static readonly Color Hud = Color.FromArgb(255, 200, 210, 220);
    private static readonly Color MeshColor = Color.FromArgb(255, 140, 165, 185);
    private static readonly Color MeshSelected = Color.FromArgb(255, 90, 210, 220);
    private static readonly Color CompSelected = Color.FromArgb(255, 255, 180, 60);
    private static readonly Color PointColor = Color.FromArgb(255, 110, 180, 210);
    private static readonly Color FaceTint = Color.FromArgb(200, 70, 190, 210);
    private static readonly Color GizmoX = Color.FromArgb(255, 220, 70, 70);
    private static readonly Color GizmoY = Color.FromArgb(255, 70, 200, 90);
    private static readonly Color GizmoZ = Color.FromArgb(255, 70, 120, 220);

    private readonly SceneSessionService _session;
    private readonly Dictionary<Guid, WireMeshCache> _wireCaches = new();
    private readonly SceneViewportCamera _camera;
    private readonly Stopwatch _sw = new();

    private int _screenWidth = 1;
    private int _screenHeight = 1;

    public SceneViewportRenderer(SceneSessionService session, SceneViewportCamera camera)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
    }

    /// <summary>Optional present-time meter (ViewportBench / diagnostics).</summary>
    public ViewportFrameMeter? FrameMeter { get; set; }

    public OrbitCameraRig Orbit => _camera.Orbit;
    public Vector3? GizmoOrigin
    {
        get => _camera.GizmoOrigin;
        set => _camera.GizmoOrigin = value;
    }

    public void Bind(RaylibHostControl host) =>
        host.FrameRendering += (_, e) => OnFrame(e.DeltaSeconds, e.ScreenWidth, e.ScreenHeight);

    public void OrbitDrag(float dx, float dy) => _camera.OrbitDrag(dx, dy);

    public void Zoom(float delta) => _camera.Zoom(delta);

    public void Fit() => _camera.Fit();

    public Ray BuildScreenRay(float localX, float localY, float controlWidth, float controlHeight) =>
        _camera.BuildScreenRay(localX, localY, controlWidth, controlHeight);

    public MeshPickHit? PickAt(float localX, float localY, float controlWidth, float controlHeight) =>
        _camera.PickAt(localX, localY, controlWidth, controlHeight);

    private void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
    {
        _ = deltaSeconds;
        _sw.Restart();
        _screenWidth = System.Math.Max(1, screenWidth);
        _screenHeight = System.Math.Max(1, screenHeight);
        Graphics.ClearBackground(Background);
        var cache = _session.Evaluator.Cache;
        _camera.SyncActiveCamera();

        var eye = _camera.Orbit.BuildEyePosition();
        var camera = Novolis.Raylib.Rendering.Camera.Perspective(eye, _camera.Orbit.Target, Vector3.UnitY, _camera.Orbit.FieldOfViewDegrees);
        World.Begin(camera);

        World.DrawGrid(32, 1f);

        var live = new HashSet<Guid>();
        foreach (var mesh in cache.EvaluatedMeshes)
        {
            live.Add(mesh.SourceId);
            DrawEvaluatedMesh(mesh);
        }

        PruneWireCaches(live);
        DrawSelectionGizmo();

        foreach (var light in cache.Lights)
            DrawLightGizmo(light);
        foreach (var cam in cache.Cameras)
            DrawCameraGizmo(cam);

        World.End();
        var edit = _session.Document.Edit;
        Graphics.DrawText(
            $"{_session.Document.Name}  {edit.Mode}/{edit.DisplayMode}  {_screenWidth}x{_screenHeight}  meshes={cache.EvaluatedMeshes.Count}",
            12, 12, 16, Hud);
        _sw.Stop();
        FrameMeter?.Record(_sw.Elapsed.TotalMilliseconds, _camera.CameraInteracting);
    }

    private void PruneWireCaches(HashSet<Guid> live)
    {
        if (_wireCaches.Count == live.Count)
            return;
        foreach (var id in _wireCaches.Keys.ToArray())
        {
            if (!live.Contains(id))
                _wireCaches.Remove(id);
        }
    }

    private void DrawEvaluatedMesh(EvaluatedMesh mesh)
    {
        var edit = _session.Document.Edit;
        var objectSelected = _session.Document.SelectionId == mesh.SourceId
                             || edit.EditMeshId == mesh.SourceId;
        var color = objectSelected ? MeshSelected : MeshColor;
        var display = edit.DisplayMode;
        var editingThis = edit.Mode != SceneEditMode.Object
                          && (edit.EditMeshId == mesh.SourceId || objectSelected);

        if (!_wireCaches.TryGetValue(mesh.SourceId, out var wire))
        {
            wire = new WireMeshCache();
            _wireCaches[mesh.SourceId] = wire;
        }

        HashSet<(int A, int B)>? highlights = null;
        if (editingThis && edit.Mode == SceneEditMode.Edge && edit.SelectedEdges.Count > 0)
            highlights = edit.SelectedEdges.ToHashSet();
        else if (editingThis && edit.Mode == SceneEditMode.Polygon && edit.SelectedFaces.Count > 0)
        {
            highlights = new HashSet<(int, int)>();
            foreach (var face in edit.SelectedFaces)
            {
                var t = face * 3;
                if (t + 2 >= mesh.Indices.Length)
                    continue;
                AddEdge(highlights, mesh.Indices[t], mesh.Indices[t + 1]);
                AddEdge(highlights, mesh.Indices[t + 1], mesh.Indices[t + 2]);
                AddEdge(highlights, mesh.Indices[t + 2], mesh.Indices[t]);
            }
        }

        wire.Draw(mesh, color, highlights, CompSelected);

        if (display == SceneDisplayMode.Isoline && editingThis && edit.Mode == SceneEditMode.Polygon && edit.SelectedFaces.Count > 0)
            wire.DrawFaceStars(mesh, edit.SelectedFaces, FaceTint);

        var showPoints = display is SceneDisplayMode.WirePoints or SceneDisplayMode.Isoline
                         || edit.Mode == SceneEditMode.Point;
        if (showPoints && mesh.Vertices.Length > 0)
        {
            // Cap point density for very large meshes — still readable, keeps frame rate.
            var pointSize = MathF.Max(0.02f, _camera.Orbit.Distance * 0.004f);
            if (mesh.Vertices.Length <= 25_000 || edit.Mode == SceneEditMode.Point)
            {
                wire.DrawPoints(
                    mesh,
                    pointSize,
                    PointColor,
                    editingThis && edit.Mode == SceneEditMode.Point ? edit.SelectedVertices : null,
                    CompSelected);
            }
        }
    }

    private static void AddEdge(HashSet<(int, int)> set, int a, int b)
    {
        if (a == b)
            return;
        set.Add(a < b ? (a, b) : (b, a));
    }

    private void DrawSelectionGizmo()
    {
        _camera.GizmoOrigin = null;
        var edit = _session.Document.Edit;
        Vector3? origin = null;
        if (edit.Mode == SceneEditMode.Object && _session.Document.SelectionId is { } sid)
        {
            var mesh = _session.Evaluator.Cache.EvaluatedMeshes.FirstOrDefault(m => m.SourceId == sid);
            if (mesh is not null && mesh.Vertices.Length > 0)
            {
                var sum = Vector3.Zero;
                foreach (var v in mesh.Vertices)
                    sum += Vector3.Transform(v, mesh.World);
                origin = sum / mesh.Vertices.Length;
            }
            else
            {
                var node = _session.Document.Find(sid);
                if (node is not null)
                    origin = node.Transform.PositionV;
            }
        }
        else if (edit.Mode != SceneEditMode.Object && edit.EditMeshId is { } mid)
        {
            var mesh = _session.Evaluator.Cache.EvaluatedMeshes.FirstOrDefault(m => m.SourceId == mid);
            if (mesh is not null)
                origin = ComponentCentroid(mesh, edit);
        }

        if (origin is null)
            return;
        _camera.GizmoOrigin = origin;
        var o = origin.Value;
        var len = MathF.Max(0.4f, _camera.Orbit.Distance * 0.06f);
        World.DrawLine(o, o + Vector3.UnitX * len, GizmoX);
        World.DrawLine(o, o + Vector3.UnitY * len, GizmoY);
        World.DrawLine(o, o + Vector3.UnitZ * len, GizmoZ);
        World.DrawSphere(o, 0.05f, CompSelected);
    }

    private static Vector3? ComponentCentroid(EvaluatedMesh mesh, MeshEditState edit)
    {
        var pts = new List<Vector3>();
        if (edit.Mode == SceneEditMode.Point)
        {
            foreach (var i in edit.SelectedVertices)
            {
                if (i >= 0 && i < mesh.Vertices.Length)
                    pts.Add(Vector3.Transform(mesh.Vertices[i], mesh.World));
            }
        }
        else if (edit.Mode == SceneEditMode.Edge)
        {
            foreach (var (a, b) in edit.SelectedEdges)
            {
                if (a >= 0 && a < mesh.Vertices.Length)
                    pts.Add(Vector3.Transform(mesh.Vertices[a], mesh.World));
                if (b >= 0 && b < mesh.Vertices.Length)
                    pts.Add(Vector3.Transform(mesh.Vertices[b], mesh.World));
            }
        }
        else if (edit.Mode == SceneEditMode.Polygon)
        {
            foreach (var f in edit.SelectedFaces)
            {
                if (f < 0 || f >= mesh.Indices.Length / 3)
                    continue;
                var i0 = mesh.Indices[f * 3];
                var i1 = mesh.Indices[f * 3 + 1];
                var i2 = mesh.Indices[f * 3 + 2];
                pts.Add(Vector3.Transform(mesh.Vertices[i0], mesh.World));
                pts.Add(Vector3.Transform(mesh.Vertices[i1], mesh.World));
                pts.Add(Vector3.Transform(mesh.Vertices[i2], mesh.World));
            }
        }

        if (pts.Count == 0)
            return null;
        var sum = Vector3.Zero;
        foreach (var p in pts)
            sum += p;
        return sum / pts.Count;
    }

    private void DrawLightGizmo(EvaluatedNode ev)
    {
        if (ev.Source is not LightNode light)
            return;
        var p = ev.WorldPosition;
        var selected = _session.Document.SelectionId == light.Id;
        var c = light.Enabled
            ? Color.FromArgb(255,
                (int)(light.Color[0] * 255),
                (int)(light.Color[1] * 255),
                (int)(light.Color[2] * 255))
            : Color.FromArgb(255, 80, 80, 80);
        if (selected)
            c = Color.FromArgb(255, 255, 200, 80);

        switch (light.LightKind)
        {
            case LightKind.Infinite:
                World.DrawLine(p, p + new Vector3(0, -1.5f, 0), c);
                World.DrawSphere(p, 0.12f, c);
                break;
            case LightKind.Spot:
                World.DrawSphere(p, 0.15f, c);
                World.DrawLine(p, p + new Vector3(0, -1.2f, 0), c);
                break;
            case LightKind.Area:
                World.DrawCube(p, MathF.Max(0.2f, light.AreaSize[0]), 0.05f, MathF.Max(0.2f, light.AreaSize[1]), c);
                break;
            default:
                World.DrawSphere(p, 0.18f, c);
                break;
        }
    }

    private void DrawCameraGizmo(EvaluatedNode ev)
    {
        var p = ev.WorldPosition;
        var selected = _session.Document.SelectionId == ev.Source.Id;
        var c = selected ? Color.FromArgb(255, 255, 160, 60) : Color.FromArgb(255, 180, 180, 200);
        World.DrawCube(p, 0.25f, 0.18f, 0.35f, c);
    }
}
