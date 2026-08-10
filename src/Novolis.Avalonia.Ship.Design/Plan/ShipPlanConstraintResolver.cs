using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Plan;

public enum ShipPlanConstraintSnapKind
{
    None,
    Free,
    Grid,
    Vertex,
    Midpoint,
    Edge,
    Ortho,
    Angle15,
    Guide,
}

public sealed record ShipPlanConstraintResult(
    float X,
    float Z,
    ShipPlanConstraintSnapKind Kind,
    IReadOnlyList<ShipPlanPaths.PlanGuideLine> Guides,
    float? SegmentLengthM = null,
    float? SegmentAngleDeg = null);

/// <summary>ArchiCAD-style PLAN constraint resolve: object snap, grid, ortho/angle locks, guides.</summary>
public static class ShipPlanConstraintResolver
{
    public static ShipPlanConstraintResult Resolve(
        float rawX,
        float rawZ,
        float? lastX,
        float? lastZ,
        ShipDesignSession session,
        DeckId? deckId,
        bool altFree,
        bool shiftOrtho,
        bool ctrlAngle,
        float objectSnapTolM,
        bool preferEdgeFirst = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        var extent = session.HasShip
            ? System.Math.Max(session.Design.Ship.LengthMeters, session.Design.Ship.BeamMeters) * 0.75f
            : 50f;

        if (altFree)
        {
            return Finish(rawX, rawZ, ShipPlanConstraintSnapKind.Free, [], lastX, lastZ);
        }

        var x = rawX;
        var z = rawZ;
        var kind = ShipPlanConstraintSnapKind.None;
        var candidates = session.HasShip && deckId is { } did
            ? ShipPlanPaths.CollectSnapCandidates(session.Design, did)
            : [];

        var angleOn = ctrlAngle || session.AngleLockEnabled;
        var orthoOn = !angleOn && (shiftOrtho || session.OrthoLocked);

        // Constraint from last point first (rubber-band direction).
        if (lastX is { } lx && lastZ is { } lz)
        {
            if (angleOn)
            {
                var a = ShipPlanPaths.ApplyAngle15(lx, lz, x, z);
                x = a[0];
                z = a[1];
                kind = ShipPlanConstraintSnapKind.Angle15;
            }
            else if (orthoOn)
            {
                var o = ShipPlanPaths.ApplyOrtho(lx, lz, x, z);
                x = o[0];
                z = o[1];
                kind = ShipPlanConstraintSnapKind.Ortho;
            }
        }

        // Opening: pull to host edge before grid.
        if (preferEdgeFirst && session.HasShip && deckId is { } edgeDeck)
        {
            if (ShipPlanPaths.TryNearestEdge(session.Design, edgeDeck, x, z, objectSnapTolM * 1.6f, out var ex, out var ez, out _))
            {
                x = ex;
                z = ez;
                kind = ShipPlanConstraintSnapKind.Edge;
                var guides = ShipPlanPaths.CollectAlignmentGuides(candidates, x, z, objectSnapTolM * 0.5f, extent);
                return Finish(x, z, kind, guides, lastX, lastZ);
            }
        }

        // Object snap: vertex → mid → edge.
        if (candidates.Count > 0 && ShipPlanPaths.TryNearestVertexOrMid(candidates, x, z, objectSnapTolM, out var hit))
        {
            x = hit.X;
            z = hit.Z;
            kind = hit.Kind == ShipPlanPaths.PlanSnapKind.Vertex
                ? ShipPlanConstraintSnapKind.Vertex
                : ShipPlanConstraintSnapKind.Midpoint;
        }
        else if (session.HasShip && deckId is { } d2
                 && ShipPlanPaths.TryNearestEdge(session.Design, d2, x, z, objectSnapTolM, out var edx, out var edz, out _))
        {
            x = edx;
            z = edz;
            kind = ShipPlanConstraintSnapKind.Edge;
        }
        else if (session.SnapEnabled && session.SnapGridMeters > 1e-6f)
        {
            // Under ortho, snap only the free axis.
            if (kind == ShipPlanConstraintSnapKind.Ortho && lastX is { } olx && lastZ is { } olz)
            {
                if (MathF.Abs(x - olx) < 1e-5f)
                {
                    z = session.Snap(z);
                }
                else
                {
                    x = session.Snap(x);
                }
            }
            else
            {
                x = session.Snap(x);
                z = session.Snap(z);
            }

            if (kind is ShipPlanConstraintSnapKind.None)
                kind = ShipPlanConstraintSnapKind.Grid;
        }

        // Re-apply direction lock after snap so grid/object don't break ortho/angle intent.
        if (lastX is { } lx2 && lastZ is { } lz2)
        {
            if (angleOn && kind is not (ShipPlanConstraintSnapKind.Vertex or ShipPlanConstraintSnapKind.Midpoint))
            {
                var a = ShipPlanPaths.ApplyAngle15(lx2, lz2, x, z);
                x = a[0];
                z = a[1];
                kind = ShipPlanConstraintSnapKind.Angle15;
            }
            else if (orthoOn && kind is not (ShipPlanConstraintSnapKind.Vertex or ShipPlanConstraintSnapKind.Midpoint))
            {
                var o = ShipPlanPaths.ApplyOrtho(lx2, lz2, x, z);
                x = o[0];
                z = o[1];
                kind = ShipPlanConstraintSnapKind.Ortho;
            }
        }

        var align = ShipPlanPaths.CollectAlignmentGuides(candidates, x, z, objectSnapTolM * 0.5f, extent);
        if (align.Count > 0 && kind is ShipPlanConstraintSnapKind.None or ShipPlanConstraintSnapKind.Grid)
            kind = ShipPlanConstraintSnapKind.Guide;

        return Finish(x, z, kind, align, lastX, lastZ);
    }

    private static ShipPlanConstraintResult Finish(
        float x,
        float z,
        ShipPlanConstraintSnapKind kind,
        IReadOnlyList<ShipPlanPaths.PlanGuideLine> guides,
        float? lastX,
        float? lastZ)
    {
        float? len = null;
        float? ang = null;
        if (lastX is { } lx && lastZ is { } lz)
        {
            var dx = x - lx;
            var dz = z - lz;
            len = MathF.Sqrt(dx * dx + dz * dz);
            ang = MathF.Atan2(dz, dx) * (180f / MathF.PI);
        }

        return new ShipPlanConstraintResult(x, z, kind, guides, len, ang);
    }
}
