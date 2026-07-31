using System.Numerics;
using Avalonia;

namespace Novolis.Avalonia.Cad.Services;

/// <summary>Screen → world helpers for drafting on a horizontal plane in the model orbit view.</summary>
public static class CadModelPick
{
    /// <summary>
    /// Cast a camera ray through a screen pixel and intersect the plane <c>Y = elevation</c>.
    /// </summary>
    public static bool TryHitElevationPlane(
        Vector3 eye,
        Vector3 target,
        float fieldOfViewDegrees,
        Size viewportSize,
        Point screen,
        float elevation,
        out Vector3 hit)
    {
        hit = default;
        if (viewportSize.Width < 1 || viewportSize.Height < 1)
            return false;

        var forward = target - eye;
        if (forward.LengthSquared() < 1e-10f)
            return false;
        forward = Vector3.Normalize(forward);

        var worldUp = Vector3.UnitY;
        var right = Vector3.Cross(forward, worldUp);
        if (right.LengthSquared() < 1e-10f)
            right = Vector3.Cross(forward, Vector3.UnitZ);
        right = Vector3.Normalize(right);
        var up = Vector3.Normalize(Vector3.Cross(right, forward));

        var aspect = (float)(viewportSize.Width / viewportSize.Height);
        var tanHalf = MathF.Tan(fieldOfViewDegrees * (MathF.PI / 360f));
        var ndcX = (float)((2.0 * screen.X / viewportSize.Width) - 1.0);
        var ndcY = (float)(1.0 - (2.0 * screen.Y / viewportSize.Height));
        var dir = Vector3.Normalize(forward + right * (ndcX * tanHalf * aspect) + up * (ndcY * tanHalf));

        if (MathF.Abs(dir.Y) < 1e-6f)
            return false;

        var t = (elevation - eye.Y) / dir.Y;
        if (t < 0.05f)
            return false;

        hit = eye + dir * t;
        return true;
    }

    /// <summary>
    /// Cast a camera ray and intersect a vertical plane through <paramref name="anchor"/>
    /// facing the camera (for Y-axis locked moves).
    /// </summary>
    public static bool TryHitVerticalPlaneFacingCamera(
        Vector3 eye,
        Vector3 target,
        float fieldOfViewDegrees,
        Size viewportSize,
        Point screen,
        Vector3 anchor,
        out Vector3 hit)
    {
        hit = default;
        if (!TryBuildRay(eye, target, fieldOfViewDegrees, viewportSize, screen, out var dir))
            return false;

        var toward = eye - anchor;
        toward.Y = 0;
        if (toward.LengthSquared() < 1e-8f)
        {
            var forward = target - eye;
            forward.Y = 0;
            toward = forward.LengthSquared() < 1e-8f ? Vector3.UnitZ : -forward;
        }

        var normal = Vector3.Normalize(toward);
        var denom = Vector3.Dot(dir, normal);
        if (MathF.Abs(denom) < 1e-6f)
            return false;

        var t = Vector3.Dot(anchor - eye, normal) / denom;
        if (t < 0.05f)
            return false;

        hit = eye + dir * t;
        return true;
    }

    private static bool TryBuildRay(
        Vector3 eye,
        Vector3 target,
        float fieldOfViewDegrees,
        Size viewportSize,
        Point screen,
        out Vector3 dir)
    {
        dir = default;
        if (viewportSize.Width < 1 || viewportSize.Height < 1)
            return false;

        var forward = target - eye;
        if (forward.LengthSquared() < 1e-10f)
            return false;
        forward = Vector3.Normalize(forward);

        var worldUp = Vector3.UnitY;
        var right = Vector3.Cross(forward, worldUp);
        if (right.LengthSquared() < 1e-10f)
            right = Vector3.Cross(forward, Vector3.UnitZ);
        right = Vector3.Normalize(right);
        var up = Vector3.Normalize(Vector3.Cross(right, forward));

        var aspect = (float)(viewportSize.Width / viewportSize.Height);
        var tanHalf = MathF.Tan(fieldOfViewDegrees * (MathF.PI / 360f));
        var ndcX = (float)((2.0 * screen.X / viewportSize.Width) - 1.0);
        var ndcY = (float)(1.0 - (2.0 * screen.Y / viewportSize.Height));
        dir = Vector3.Normalize(forward + right * (ndcX * tanHalf * aspect) + up * (ndcY * tanHalf));
        return true;
    }
}
