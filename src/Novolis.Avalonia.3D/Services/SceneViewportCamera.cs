using System.Numerics;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;
using Novolis.Simulation.View;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Shared orbit + pick for all viewport backends.</summary>
public sealed class SceneViewportCamera
{
    private readonly SceneSessionService _session;
    private readonly OrbitCameraRig _orbit = new()
    {
        Target = new Vector3(0f, 1f, 0f),
        Distance = 12f,
        MinDistance = 1f,
        MaxDistance = 200f,
        Yaw = 0.6f,
        Pitch = 0.4f,
        FieldOfViewDegrees = 45f,
    };

    public SceneViewportCamera(SceneSessionService session) =>
        _session = session ?? throw new ArgumentNullException(nameof(session));

    public OrbitCameraRig Orbit => _orbit;
    public Vector3? GizmoOrigin { get; set; }

    /// <summary>When false, orbit/zoom own the view (ignore document ActiveCamera).</summary>
    public bool FollowDocumentCamera { get; set; } = true;

    /// <summary>True briefly after orbit/zoom — used by frame meters for camera-motion benchmarks.</summary>
    public bool CameraInteracting { get; private set; }

    /// <summary>Raised after orbit/zoom/fit so shared-camera panes can present together.</summary>
    public event Action? Changed;

    private long _interactUntilTick;

    public void OrbitDrag(float dx, float dy)
    {
        _orbit.AddLookDelta(dx * 0.01f, dy * 0.01f);
        MarkInteracting();
        Changed?.Invoke();
    }

    public void Zoom(float delta)
    {
        _orbit.AdjustDistance(delta > 0 ? -1.2f : 1.2f);
        MarkInteracting();
        Changed?.Invoke();
    }

    public void Fit()
    {
        _orbit.Target = new Vector3(0f, 1f, 0f);
        _orbit.Distance = 12f;
        _orbit.Yaw = 0.6f;
        _orbit.Pitch = 0.4f;
        MarkInteracting();
        Changed?.Invoke();
    }

    public void SyncActiveCamera()
    {
        RefreshInteracting();
        if (!FollowDocumentCamera)
            return;
        if (_session.Document.ActiveCameraId is not { } id)
            return;
        var cam = _session.Evaluator.Cache.Cameras.FirstOrDefault(c => c.Source.Id == id);
        if (cam?.Source is not CameraNode node)
            return;
        var target = new Vector3(node.Target[0], node.Target[1], node.Target[2]);
        _orbit.Target = target;
        _orbit.Distance = MathF.Max(1f, Vector3.Distance(cam.WorldPosition, target));
    }

    private void MarkInteracting()
    {
        CameraInteracting = true;
        _interactUntilTick = Environment.TickCount64 + 250;
    }

    private void RefreshInteracting()
    {
        if (CameraInteracting && Environment.TickCount64 > _interactUntilTick)
            CameraInteracting = false;
    }

    public Matrix4x4 BuildViewProjection(float aspect)
    {
        var eye = _orbit.BuildEyePosition();
        var view = Matrix4x4.CreateLookAt(eye, _orbit.Target, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(
            _orbit.FieldOfViewDegrees * (MathF.PI / 180f),
            MathF.Max(0.01f, aspect),
            0.05f,
            500f);
        return view * proj;
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
}
