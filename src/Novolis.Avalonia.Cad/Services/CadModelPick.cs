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
}
