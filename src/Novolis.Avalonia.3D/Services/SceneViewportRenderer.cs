using System.Drawing;
using System.Numerics;
using Novolis.Avalonia.Raylib;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;
using Novolis.Raylib.Rendering;
using Novolis.Rendering.Presentation.Silk;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Draws scene meshes + light/camera gizmos into a Raylib host.</summary>
public sealed class SceneViewportRenderer
{
    private static readonly Color Background = Color.FromArgb(255, 18, 24, 32);
    private static readonly Color Grid = Color.FromArgb(255, 40, 55, 70);
    private static readonly Color Hud = Color.FromArgb(255, 200, 210, 220);

    private readonly SceneSessionService _session;
    private readonly SilkOrbitCamera _orbit = new()
    {
        Target = new Vector3(0f, 1f, 0f),
        Distance = 10f,
        MinDistance = 1f,
        MaxDistance = 200f,
        Yaw = 0.6f,
        Pitch = 0.4f,
        FieldOfViewDegrees = 45f,
    };

    public SceneViewportRenderer(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public SilkOrbitCamera Orbit => _orbit;

    public void Bind(RaylibHostControl host) =>
        host.FrameRendering += (_, e) => OnFrame(e.DeltaSeconds, e.ScreenWidth, e.ScreenHeight);

    public void OrbitDrag(float dx, float dy) =>
        _orbit.AddLookDelta(dx * 0.01f, dy * 0.01f);

    public void Zoom(float delta) =>
        _orbit.AdjustDistance(delta > 0 ? -1.2f : 1.2f);

    public void Fit()
    {
        _orbit.Target = new Vector3(0f, 1f, 0f);
        _orbit.Distance = 10f;
        _orbit.Yaw = 0.6f;
        _orbit.Pitch = 0.4f;
    }

    private void OnFrame(float deltaSeconds, int screenWidth, int screenHeight)
    {
        _ = deltaSeconds;
        _ = screenWidth;
        _ = screenHeight;
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

        foreach (var mesh in cache.Meshes)
            DrawMesh(mesh);
        foreach (var derived in MeshStackEvaluator.Expand(_session.Document, cache))
        {
            var p = Vector3.Transform(Vector3.Zero, derived.World);
            World.DrawCube(p, 0.6f, 0.6f, 0.6f, Color.FromArgb(255, 90, 140, 110));
        }

        foreach (var light in cache.Lights)
            DrawLightGizmo(light);
        foreach (var cam in cache.Cameras)
            DrawCameraGizmo(cam);

        World.End();
        Graphics.DrawText($"{_session.Document.Name}  lights={cache.Lights.Count}", 12, 12, 16, Hud);
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

    private void DrawMesh(EvaluatedNode mesh)
    {
        if (mesh.Source is not MeshNode mn)
            return;
        var p = mesh.WorldPosition;
        var selected = _session.Document.SelectionId == mn.Id;
        var color = selected ? Color.FromArgb(255, 80, 180, 200) : Color.FromArgb(255, 120, 130, 145);
        var sx = MathF.Max(0.05f, mn.Size[0]);
        var sy = MathF.Max(0.05f, mn.Size[1]);
        var sz = MathF.Max(0.05f, mn.Size[2]);
        if (mn.Primitive == MeshPrimitiveKind.Sphere)
            World.DrawSphere(p, MathF.Max(sx, MathF.Max(sy, sz)) * 0.5f, color);
        else
            World.DrawCube(p, sx, sy, sz, color);
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
