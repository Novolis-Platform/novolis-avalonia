using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Plan;

public sealed record ShipPlanConstraintResult(
    float X,
    float Z,
    ShipPlanConstraintSnapKind Kind,
    IReadOnlyList<ShipPlanPaths.PlanGuideLine> Guides,
    float? SegmentLengthM = null,
    float? SegmentAngleDeg = null);
