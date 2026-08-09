using System.Drawing;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
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

    /// <summary>
    /// Paints space/opening <see cref="CadEntity.Color"/> so Cad plan view can fill sealed vs venting.
    /// </summary>
    public static void Apply(CadDocument document, ShipTopologyResult topology)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(topology);
        foreach (var space in ShipCad.Spaces(document))
        {
            var c = SpaceFill(space, topology);
            space.Color = [c.R / 255f, c.G / 255f, c.B / 255f];
        }

        foreach (var oid in topology.OrphanOpeningIds)
        {
            var opening = document.Entities.FirstOrDefault(e => e.Id == oid);
            if (opening is null)
                continue;
            var c = OrphanOpening;
            opening.Color = [c.R / 255f, c.G / 255f, c.B / 255f];
        }
    }
}
