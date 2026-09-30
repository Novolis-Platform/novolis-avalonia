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
