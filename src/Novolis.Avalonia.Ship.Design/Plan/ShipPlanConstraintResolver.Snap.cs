using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Plan;

public static partial class ShipPlanConstraintResolver
{
    static (float X, float Z, ShipPlanConstraintSnapKind Kind) ApplyDirectionFromAnchor(
        float x,
        float z,
        ShipPlanConstraintSnapKind kind,
        float? lastX,
        float? lastZ,
        bool angleOn,
        bool orthoOn)
    {
        if (lastX is not { } lx || lastZ is not { } lz)
            return (x, z, kind);

        if (angleOn)
        {
            var a = ShipPlanPaths.ApplyAngle15(lx, lz, x, z);
            return (a[0], a[1], ShipPlanConstraintSnapKind.Angle15);
        }

        if (orthoOn)
        {
            var o = ShipPlanPaths.ApplyOrtho(lx, lz, x, z);
            return (o[0], o[1], ShipPlanConstraintSnapKind.Ortho);
        }

        return (x, z, kind);
    }

    static bool TryPreferEdgeSnap(
        ShipDesignSession session,
        DeckId deckId,
        float x,
        float z,
        float objectSnapTolM,
        float extent,
        IReadOnlyList<ShipPlanPaths.PlanSnapCandidate> candidates,
        float? lastX,
        float? lastZ,
        out ShipPlanConstraintResult result)
    {
        if (!ShipPlanPaths.TryNearestEdge(session.Design, deckId, x, z, objectSnapTolM * 1.6f, out var ex, out var ez, out _))
        {
            result = default!;
            return false;
        }

        var guides = ShipPlanPaths.CollectAlignmentGuides(candidates, ex, ez, objectSnapTolM * 0.5f, extent);
        result = Finish(ex, ez, ShipPlanConstraintSnapKind.Edge, guides, lastX, lastZ);
        return true;
    }

    static (float X, float Z, ShipPlanConstraintSnapKind Kind) SnapToObjectsOrGrid(
        float x,
        float z,
        ShipPlanConstraintSnapKind kind,
        ShipDesignSession session,
        DeckId? deckId,
        IReadOnlyList<ShipPlanPaths.PlanSnapCandidate> candidates,
        float objectSnapTolM,
        float? lastX,
        float? lastZ)
    {
        if (candidates.Count > 0
            && ShipPlanPaths.TryNearestVertexOrMid(candidates, x, z, objectSnapTolM, out var hit))
        {
            return (
                hit.X,
                hit.Z,
                hit.Kind == ShipPlanPaths.PlanSnapKind.Vertex
                    ? ShipPlanConstraintSnapKind.Vertex
                    : ShipPlanConstraintSnapKind.Midpoint);
        }

        if (deckId is { } d2
            && ShipPlanPaths.TryNearestEdge(session.Design, d2, x, z, objectSnapTolM, out var edx, out var edz, out _))
            return (edx, edz, ShipPlanConstraintSnapKind.Edge);

        return SnapToGrid(x, z, kind, session, lastX, lastZ);
    }

    static (float X, float Z, ShipPlanConstraintSnapKind Kind) SnapToGrid(
        float x,
        float z,
        ShipPlanConstraintSnapKind kind,
        ShipDesignSession session,
        float? lastX,
        float? lastZ)
    {
        if (!session.SnapEnabled || session.SnapGridMeters <= 1e-6f)
            return (x, z, kind);

        if (kind == ShipPlanConstraintSnapKind.Ortho && lastX is { } olx && lastZ is { } olz)
        {
            if (MathF.Abs(x - olx) < 1e-5f)
                z = session.Snap(z);
            else
                x = session.Snap(x);
        }
        else
        {
            x = session.Snap(x);
            z = session.Snap(z);
        }

        if (kind is ShipPlanConstraintSnapKind.None)
            kind = ShipPlanConstraintSnapKind.Grid;

        return (x, z, kind);
    }

    static (float X, float Z, ShipPlanConstraintSnapKind Kind) ReapplyDirectionAfterSnap(
        float x,
        float z,
        ShipPlanConstraintSnapKind kind,
        float? lastX,
        float? lastZ,
        bool angleOn,
        bool orthoOn)
    {
        if (lastX is not { } lx2 || lastZ is not { } lz2)
            return (x, z, kind);

        if (kind is ShipPlanConstraintSnapKind.Vertex or ShipPlanConstraintSnapKind.Midpoint)
            return (x, z, kind);

        if (angleOn)
        {
            var a = ShipPlanPaths.ApplyAngle15(lx2, lz2, x, z);
            return (a[0], a[1], ShipPlanConstraintSnapKind.Angle15);
        }

        if (orthoOn)
        {
            var o = ShipPlanPaths.ApplyOrtho(lx2, lz2, x, z);
            return (o[0], o[1], ShipPlanConstraintSnapKind.Ortho);
        }

        return (x, z, kind);
    }
}
