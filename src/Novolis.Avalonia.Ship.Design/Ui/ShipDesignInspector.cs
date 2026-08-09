using System.Text;
using Novolis.Avalonia.Ship.Design.Grips;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

public static class ShipDesignInspector
{
    public static string Format(ShipDesignSession session)
    {
        var d = session.Design;
        var val = session.Validation;
        var sb = new StringBuilder();
        sb.AppendLine($"Ship: {d.Ship.Name}");
        sb.AppendLine($"Envelope: {d.Ship.LengthMeters:0.##} × {d.Ship.BeamMeters:0.##} × {d.Ship.HeightMeters:0.##} m");
        sb.AppendLine($"Workspace: {session.Workspace}");
        sb.AppendLine($"Hull: {d.Hull.Generator} · thick {ShipLengths.ToMeters(d.Hull.Thickness):0.###} m · {d.Hull.Material}");
        sb.AppendLine($"Decks: {d.Decks.Count} · Frames: {d.Frames.Count} · Longs: {d.Longitudinals.Count}");
        sb.AppendLine($"Bulkheads: {d.Bulkheads.Count} · Compartments: {d.Compartments.Count}");
        sb.AppendLine($"Passages: {d.Passages.Count} · Openings: {d.Openings.Count} · Equipment: {d.Equipment.Count}");
        sb.AppendLine($"Cutouts: {d.Cutouts.Count}");
        sb.AppendLine($"Validation: {(val.Ok ? "OK" : "FAIL")} ({val.Issues.Count}) — continuous");
        foreach (var i in val.Issues.Take(10))
            sb.AppendLine($"  [{i.Severity}] {i.Code}: {i.Message}");

        sb.AppendLine();
        sb.AppendLine("Selection:");
        if (session.SelectedObjectId is null)
        {
            sb.AppendLine("  (none)");
            return sb.ToString();
        }

        var id = session.SelectedObjectId.Value.Value;
        AppendSelection(sb, d, id);

        var grips = ShipGripCatalog.ForSelection(d, session.SelectedObjectId);
        if (grips.Count > 0)
        {
            sb.AppendLine("Grips:");
            foreach (var g in grips.Take(12))
                sb.AppendLine($"  {g.Kind} {g.Label} @ ({g.X:0.##},{g.Y:0.##},{g.Z:0.##})");
        }

        if (session.ShowStructuralOverlays)
        {
            var related = d.Cutouts.Where(c =>
                c.HostId.Value == id || c.SourceId.Value == id).Take(8).ToList();
            if (related.Count > 0)
            {
                sb.AppendLine("Cutout overlays:");
                foreach (var c in related)
                    sb.AppendLine($"  {c.Purpose}: {c.SourceId.Value:N} → {c.HostId.Value:N}");
            }
        }

        return sb.ToString();
    }

    private static void AppendSelection(StringBuilder sb, ShipDesign d, Guid id)
    {
        if (d.Hull.Id.Value == id)
        {
            sb.AppendLine($"  Hull {id:N}");
            sb.AppendLine($"  Entities: {d.Hull.Geometry.Entities.Count}");
            return;
        }

        var deck = d.Decks.FirstOrDefault(x => x.Id.Value == id);
        if (deck is not null)
        {
            sb.AppendLine($"  Deck '{deck.Name}' elev {ShipLengths.ToMeters(deck.Elevation):0.###} m");
            return;
        }

        var frame = d.Frames.FirstOrDefault(x => x.Id.Value == id);
        if (frame is not null)
        {
            sb.AppendLine($"  Frame '{frame.Name}' station {ShipLengths.ToMeters(frame.Station):0.###} m");
            return;
        }

        var bh = d.Bulkheads.FirstOrDefault(x => x.Id.Value == id);
        if (bh is not null)
        {
            sb.AppendLine($"  Bulkhead '{bh.Name}' thick {ShipLengths.ToMeters(bh.Thickness):0.###} m");
            return;
        }

        var passage = d.Passages.FirstOrDefault(x => x.Id.Value == id);
        if (passage is not null)
        {
            sb.AppendLine($"  Passage '{passage.Name}' width {ShipLengths.ToMeters(passage.Width):0.###} m");
            return;
        }

        var opening = d.Openings.FirstOrDefault(x => x.Id.Value == id);
        if (opening is not null)
        {
            sb.AppendLine($"  Opening '{opening.Name}' {opening.Kind} host {opening.HostId}");
            return;
        }

        sb.AppendLine($"  Object {id:N}");
    }
}
