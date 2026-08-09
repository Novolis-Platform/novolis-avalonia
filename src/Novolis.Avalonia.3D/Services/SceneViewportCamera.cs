using System.Numerics;
using Novolis.Avalonia._3D.Session;
using Novolis._3D;
using Novolis.Simulation.View;

namespace Novolis.Avalonia._3D.Services;

/// <summary>Shared orbit + pick for all viewport backends.</summary>
public sealed class SceneViewportCamera
{
    private const float CadPitchLimit = MathF.PI * 0.49f;

    private readonly SceneSessionService _session;
    private readonly OrbitCameraRig _orbit = new()
    {
        Target = new Vector3(0f, 1f, 0f),
        Distance = 12f,
        MinDistance = 0.5f,
        MaxDistance = 500f,
        MinPitch = -CadPitchLimit,
        Yaw = 0.6f,
        Pitch = 0.4f,
        FieldOfViewDegrees = 45f,
    };

    public SceneViewportCamera(SceneSessionService session) =>
        _session = session ?? throw new ArgumentNullException(nameof(session));

    public OrbitCameraRig Orbit => _orbit;
    public Vector3? GizmoOrigin { get; set; }

    /// <summary>
    /// When true, <see cref="ApplyActiveCameraFromDocument"/> may pull the orbit from the document camera.
    /// User orbit/zoom/fit always own the view afterward (CAD navigation is not locked to the camera node).
    /// </summary>
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

    public void Pan(float dx, float dy)
    {
        var eye = _orbit.BuildEyePosition();
        var forward = Vector3.Normalize(_orbit.Target - eye);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        if (right.LengthSquared() < 1e-8f)
            right = Vector3.UnitX;
        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var scale = _orbit.Distance * 0.0025f;
        var delta = (-right * dx + up * dy) * scale;
        _orbit.SnapTarget(_orbit.Target + delta);
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
        var meshes = _session.Evaluator.Cache.EvaluatedMeshes;
        if (meshes.Count == 0)
        {
            _orbit.SnapTarget(new Vector3(0f, 1f, 0f));
            _orbit.Distance = 12f;
            _orbit.Yaw = 0.6f;
            _orbit.Pitch = 0.4f;
            MarkInteracting();
            Changed?.Invoke();
            return;
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var mesh in meshes)
        {
            foreach (var v in mesh.Vertices)
            {
                var w = Vector3.Transform(v, mesh.World);
                min = Vector3.Min(min, w);
                max = Vector3.Max(max, w);
            }
        }

        var center = (min + max) * 0.5f;
        var radius = Vector3.Distance(min, max) * 0.5f;
        _orbit.SnapTarget(center);
        _orbit.Distance = System.Math.Clamp(radius * 2.4f, 4f, _orbit.MaxDistance);
        _orbit.Yaw = 0.75f;
        _orbit.Pitch = 0.35f;
        MarkInteracting();
        Changed?.Invoke();
    }

    /// <summary>
    /// Legacy hook from presenters — no longer overwrites the orbit every frame (that locked navigation).
    /// Only refreshes the interacting flag.
    /// </summary>
    public void SyncActiveCamera() => RefreshInteracting();

    /// <summary>Snap the orbit to the document active camera (look-through). Called on Set Active Camera.</summary>
    public void ApplyActiveCameraFromDocument()
    {
        if (!FollowDocumentCamera)
            return;
        if (_session.Document.ActiveCameraId is not { } id)
            return;
        var cam = _session.Evaluator.Cache.Cameras.FirstOrDefault(c => c.Source.Id == id);
        if (cam?.Source is not CameraNode node)
            return;
        ApplyFromEyeAndTarget(cam.WorldPosition, new Vector3(node.Target[0], node.Target[1], node.Target[2]), node.FovDeg);
    }

    /// <summary>Write current viewport orbit into a camera node (eye + target + FOV).</summary>
    public void WriteToCameraNode(CameraNode camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        var eye = _orbit.BuildEyePosition();
        var target = _orbit.Target;
        camera.Transform.Position = [eye.X, eye.Y, eye.Z];
        camera.Target = [target.X, target.Y, target.Z];
        camera.FovDeg = _orbit.FieldOfViewDegrees;

        var forward = target - eye;
        if (forward.LengthSquared() > 1e-8f)
        {
            forward = Vector3.Normalize(forward);
            var yaw = MathF.Atan2(forward.X, forward.Z) * (180f / MathF.PI);
            var pitch = MathF.Asin(System.Math.Clamp(forward.Y, -1f, 1f)) * (180f / MathF.PI);
            camera.Transform.RotationDeg = [-pitch, yaw, 0f];
        }
    }

    public void ApplyFromEyeAndTarget(Vector3 eye, Vector3 target, float? fovDeg = null)
    {
        _orbit.SnapTarget(target);
        var delta = eye - target;
        var dist = delta.Length();
        _orbit.Distance = System.Math.Clamp(dist, _orbit.MinDistance, _orbit.MaxDistance);
        if (dist > 1e-5f)
        {
            var dir = delta / dist;
            _orbit.Yaw = MathF.Atan2(dir.X, dir.Z);
            _orbit.Pitch = System.Math.Clamp(MathF.Asin(System.Math.Clamp(dir.Y, -1f, 1f)), _orbit.MinPitch, CadPitchLimit);
        }

        if (fovDeg is > 1f and < 170f)
            _orbit.FieldOfViewDegrees = fovDeg.Value;

        MarkInteracting();
        Changed?.Invoke();
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
            2000f);
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
