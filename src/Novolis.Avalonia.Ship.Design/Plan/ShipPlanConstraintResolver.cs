using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Plan;

/// <summary>ArchiCAD-style PLAN constraint resolve: object snap, grid, ortho/angle locks, guides.</summary>
public static partial class ShipPlanConstraintResolver
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
            return Finish(rawX, rawZ, ShipPlanConstraintSnapKind.Free, [], lastX, lastZ);

        var candidates = session.HasShip && deckId is { } did
            ? ShipPlanPaths.CollectSnapCandidates(session.Design, did)
            : [];

        var angleOn = ctrlAngle || session.AngleLockEnabled;
        var orthoOn = !angleOn && (shiftOrtho || session.OrthoLocked);

        var (x, z, kind) = ApplyDirectionFromAnchor(rawX, rawZ, ShipPlanConstraintSnapKind.None, lastX, lastZ, angleOn, orthoOn);

        if (preferEdgeFirst && session.HasShip && deckId is { } edgeDeck
            && TryPreferEdgeSnap(session, edgeDeck, x, z, objectSnapTolM, extent, candidates, lastX, lastZ, out var edgeHit))
            return edgeHit;

        (x, z, kind) = SnapToObjectsOrGrid(x, z, kind, session, deckId, candidates, objectSnapTolM, lastX, lastZ);
        (x, z, kind) = ReapplyDirectionAfterSnap(x, z, kind, lastX, lastZ, angleOn, orthoOn);

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
