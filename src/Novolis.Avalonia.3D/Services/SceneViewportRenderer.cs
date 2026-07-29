using System.Drawing;
using System.Numerics;
using Novolis.Avalonia.Raylib;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;
using Novolis.Raylib.Rendering;
using Novolis.Rendering.Presentation.Silk;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Draws evaluated triangle meshes + light/camera gizmos + component highlights.</summary>
public sealed class SceneViewportRenderer
{
    private static readonly Color Background = Color.FromArgb(255, 18, 24, 32);
    private static readonly Color Grid = Color.FromArgb(255, 40, 55, 70);
    private static readonly Color Hud = Color.FromArgb(255, 200, 210, 220);
    private static readonly Color MeshColor = Color.FromArgb(255, 120, 145, 160);
    private static readonly Color MeshSelected = Color.FromArgb(255, 80, 200, 210);
    private static readonly Color CompSelected = Color.FromArgb(255, 255, 180, 60);
    private static readonly Color PointColor = Color.FromArgb(255, 90, 160, 190);
    private static readonly Color FaceTint = Color.FromArgb(120, 60, 180, 200);
    private static readonly Color GizmoX = Color.FromArgb(255, 220, 70, 70);
    private static readonly Color GizmoY = Color.FromArgb(255, 70, 200, 90);
    private static readonly Color GizmoZ = Color.FromArgb(255, 70, 120, 220);

    private readonly SceneSessionService _session;
    private readonly SilkOrbitCamera _orbit = new()
    {
        Target = new Vector3(0f, 1f, 0f),
        Distance = 12f,
        MinDistance = 1f,
        MaxDistance = 200f,
        Yaw = 0.6f,
        Pitch = 0.4f,
        FieldOfViewDegrees = 45f,
    };

    private int _screenWidth = 1;
    private int _screenHeight = 1;
    private Vector3? _gizmoOrigin;

    public SceneViewportRenderer(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public SilkOrbitCamera Orbit => _orbit;
    public Vector3? GizmoOrigin => _gizmoOrigin;

    public void Bind(RaylibHostControl host) =>
        host.FrameRendering += (_, e) => OnFrame(e.DeltaSeconds, e.ScreenWidth, e.ScreenHeight);

    public void OrbitDrag(float dx, float dy) =>
        _orbit.AddLookDelta(dx * 0.01f, dy * 0.01f);

    public void Zoom(float delta) =>
        _orbit.AdjustDistance(delta > 0 ? -1.2f : 1.2f);

    public void Fit()
    {
        _orbit.Target = new Vector3(0f, 1f, 0f);
        _orbit.Distance = 12f;
        _orbit.Yaw = 0.6f;
        _orbit.Pitch = 0.4f;
    }

    public Ray BuildScreenRay(float localX, float localY, float controlWidth, float controlHeight)
    {
        var aspect = controlWidth <= 0 ? 1f : (float)(controlWidth / System.Math.Max(1.0, controlHeight));
        var ndcX = (float)(2.0 * (localX / System.Math.Max(1.0, controlWidth)) - 1.0);
        var ndcY = (float)(1.0 - 2.0 * (localY / System.Math.Max(1.0, controlHeight)));
        var eye = _orbit.BuildEyePosition();
        return MeshPicker.ScreenRay(eye, _orbit.Target, Vector3.UnitY, _orbit.FieldOfViewDegrees, aspect, ndcX, ndcY);
    }

    public MeshPickHit? PickAt(float localX, float localY, float controlWidth, float controlHeight)
    {
        var ray = BuildScreenRay(localX, localY, controlWidth, controlHeight);
        var mode = _session.Document.Edit.Mode;
        var tol = MathF.Max(0.08f, _orbit.Distance * 0.012f);
        return MeshPicker.Pick(_session.Evaluator.Cache.EvaluatedMeshes, ray, mode, pointPixelTolerance: tol, edgePixelTolerance: tol);
    }

    private void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
    {
        _ = deltaSeconds;
        _screenWidth = System.Math.Max(1, screenWidth);
        _screenHeight = System.Math.Max(1, screenHeight);
        Graphics.ClearBackground(Background);
        var cache = _session.Evaluator.Cache;
        ApplyActiveCamera(cache);

        var eye = _orbit.BuildEyePosition();
        var camera = Camera.Perspective(eye, _orbit.Target, Vector3.UnitY, _orbit.FieldOfViewDegrees);
        World.Begin(camera);

        for (var i = -8; i <= 8; i++)
        {
            World.DrawLine(new Vector3(i, 0, -8), new Vector3(i, 0, 8), Grid);
            World.DrawLine(new Vector3(-8, 0, i), new Vector3(8, 0, i), Grid);
        }

        foreach (var mesh in cache.EvaluatedMeshes)
            DrawEvaluatedMesh(mesh);

        DrawSelectionGizmo();

        foreach (var light in cache.Lights)
            DrawLightGizmo(light);
        foreach (var cam in cache.Cameras)
            DrawCameraGizmo(cam);

        World.End();
        var edit = _session.Document.Edit;
        Graphics.DrawText(
            $"{_session.Document.Name}  {edit.Mode}/{edit.DisplayMode}  sel={edit.SelectionCount}  meshes={cache.EvaluatedMeshes.Count}",
            12, 12, 16, Hud);
    }

    private void ApplyActiveCamera(LookCache cache)
    {
        if (_session.Document.ActiveCameraId is not { } id)
            return;
        var cam = cache.Cameras.FirstOrDefault(c => c.Source.Id == id);
        if (cam?.Source is not CameraNode node)
            return;
        var target = new Vector3(node.Target[0], node.Target[1], node.Target[2]);
        var pos = cam.WorldPosition;
        _orbit.Target = target;
        _orbit.Distance = MathF.Max(1f, Vector3.Distance(pos, target));
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

        for (var t = 0; t < mesh.Indices.Length; t += 3)
        {
            var face = t / 3;
            var a = Vector3.Transform(mesh.Vertices[mesh.Indices[t]], mesh.World);
            var b = Vector3.Transform(mesh.Vertices[mesh.Indices[t + 1]], mesh.World);
            var c = Vector3.Transform(mesh.Vertices[mesh.Indices[t + 2]], mesh.World);
            var faceSelected = editingThis && edit.Mode == SceneEditMode.Polygon && edit.SelectedFaces.Contains(face);
            var edgeColor = faceSelected ? CompSelected : color;

            World.DrawLine(a, b, EdgeSelected(mesh.Indices[t], mesh.Indices[t + 1], editingThis) ? CompSelected : edgeColor);
            World.DrawLine(b, c, EdgeSelected(mesh.Indices[t + 1], mesh.Indices[t + 2], editingThis) ? CompSelected : edgeColor);
            World.DrawLine(c, a, EdgeSelected(mesh.Indices[t + 2], mesh.Indices[t], editingThis) ? CompSelected : edgeColor);

            if (display == SceneDisplayMode.Isoline && faceSelected)
            {
                var mid = (a + b + c) / 3f;
                World.DrawLine(a, mid, FaceTint);
                World.DrawLine(b, mid, FaceTint);
                World.DrawLine(c, mid, FaceTint);
            }
        }

        if (display is SceneDisplayMode.WirePoints or SceneDisplayMode.Isoline || edit.Mode == SceneEditMode.Point)
        {
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                var p = Vector3.Transform(mesh.Vertices[i], mesh.World);
                var selected = editingThis && edit.SelectedVertices.Contains(i);
                World.DrawSphere(p, selected ? 0.06f : 0.035f, selected ? CompSelected : PointColor);
            }
        }

        bool EdgeSelected(int a, int b, bool active)
        {
            if (!active || edit.Mode != SceneEditMode.Edge)
                return false;
            var key = a < b ? (a, b) : (b, a);
            return edit.SelectedEdges.Contains(key);
        }
    }

    private void DrawSelectionGizmo()
    {
        _gizmoOrigin = null;
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
        _gizmoOrigin = origin;
        var o = origin.Value;
        var len = MathF.Max(0.4f, _orbit.Distance * 0.06f);
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
