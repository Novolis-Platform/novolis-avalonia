using System.Drawing;
using Novolis.Cad.Primitives;
using Novolis.Ship.Topology;

namespace Novolis.Avalonia.Ship.Services;

/// <summary>Colors for airtight overlay (sealed vs venting spaces).</summary>
public static class ShipAirtightOverlay
{
    public static readonly Color SealedFill = Color.FromArgb(90, 40, 120, 90);
    public static readonly Color VentingFill = Color.FromArgb(110, 160, 70, 50);
    public static readonly Color OrphanOpening = Color.FromArgb(255, 220, 90, 60);

    public static Color SpaceFill(CadEntity space, ShipTopologyResult topology) =>
        topology.IsSpaceSealed(space.Id) ? SealedFill : VentingFill;
}
