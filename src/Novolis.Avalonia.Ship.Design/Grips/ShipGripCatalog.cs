using Novolis.Cad.Primitives;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Grips;

public enum ShipGripKind
{
    Endpoint,
    Segment,
    Vertex,
    InsertVertex,
    Thickness,
    Width,
    Elevation,
    Station,
    Slide,
    Resize,
}

/// <summary>Baseline §20 grip descriptors for selected semantic objects (PLAN direct manipulation).</summary>
public sealed record ShipGrip(
    ShipObjectId ObjectId,
    ShipGripKind Kind,
    float X,
    float Y,
    float Z,
    string Label);

public static class ShipGripCatalog
{
    public static IReadOnlyList<ShipGrip> ForSelection(ShipDesign design, ShipObjectId? selected)
    {
        ArgumentNullException.ThrowIfNull(design);
        if (selected is null)
            return [];

        var id = selected.Value;
        var grips = new List<ShipGrip>();

        var deck = design.Decks.FirstOrDefault(d => d.Id.Value == id.Value);
        if (deck is not null)
        {
            var elev = ShipLengths.ToMeters(deck.Elevation);
            grips.Add(new ShipGrip(id, ShipGripKind.Elevation, 0f, elev, 0f, "elevation"));
            return grips;
        }

        var frame = design.Frames.FirstOrDefault(f => f.Id.Value == id.Value);
        if (frame is not null)
        {
            var station = ShipLengths.ToMeters(frame.Station);
            grips.Add(new ShipGrip(id, ShipGripKind.Station, 0f, design.Ship.HeightMeters * 0.5f, station, "station"));
            return grips;
        }

        var bh = design.Bulkheads.FirstOrDefault(b => b.Id.Value == id.Value);
        if (bh is not null)
        {
            AppendPathGrips(grips, id, bh.Geometry, includeThickness: true, thicknessM: ShipLengths.ToMeters(bh.Thickness));
            return grips;
        }

        var compartment = design.Compartments.FirstOrDefault(c => c.Id.Value == id.Value);
        if (compartment is not null)
        {
            AppendPathGrips(grips, id, compartment.Geometry, includeThickness: false, thicknessM: 0f, vertexOnly: true);
            return grips;
        }

        var passage = design.Passages.FirstOrDefault(p => p.Id.Value == id.Value);
        if (passage is not null)
        {
            AppendPathGrips(grips, id, passage.Geometry, includeThickness: false, thicknessM: 0f);
            var path = ShipPlanPaths.ExtractPathXz(passage.Geometry);
            var mid = ShipPlanPaths.PointAlong(path, 0.5f);
            var halfW = ShipLengths.ToMeters(passage.Width) * 0.5f;
            grips.Add(new ShipGrip(
                id,
                ShipGripKind.Width,
                mid[0] + halfW,
                ShipLengths.ToMeters(passage.Height) * 0.5f,
                mid[1],
                $"width={ShipLengths.ToMeters(passage.Width):0.###}"));
            return grips;
        }

        var opening = design.Openings.FirstOrDefault(o => o.Id.Value == id.Value);
        if (opening is not null)
        {
            var center = opening.Geometry.Entities.FirstOrDefault()?.Center;
            var x = center is { Length: >= 1 } ? center[0] : 0f;
            var y = center is { Length: >= 2 } ? center[1] : 0f;
            var z = center is { Length: >= 3 } ? center[2] : 0f;
            grips.Add(new ShipGrip(id, ShipGripKind.Slide, x, y, z, "slide"));
            grips.Add(new ShipGrip(id, ShipGripKind.Resize, x + 0.5f, y, z, "resize"));
        }

        return grips;
    }

    private static void AppendPathGrips(
        List<ShipGrip> grips,
        ShipObjectId id,
        CadDocument geometry,
        bool includeThickness,
        float thicknessM,
        bool vertexOnly = false)
    {
        var points = new List<(float X, float Y, float Z)>();
        foreach (var e in geometry.Entities)
        {
            if (e.A is { Length: >= 3 })
                points.Add((e.A[0], e.A[1], e.A[2]));
            if (e.B is { Length: >= 3 })
                points.Add((e.B[0], e.B[1], e.B[2]));
            if (e.Points is { Count: > 0 })
            {
                foreach (var p in e.Points)
                {
                    if (p.Length >= 3)
                        points.Add((p[0], p[1], p[2]));
                }
            }
        }

        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            grips.Add(new ShipGrip(id, vertexOnly ? ShipGripKind.Vertex : ShipGripKind.Endpoint, p.X, p.Y, p.Z, $"p{i}"));
            if (!vertexOnly && i + 1 < points.Count)
            {
                var q = points[i + 1];
                grips.Add(new ShipGrip(
                    id,
                    ShipGripKind.Segment,
                    (p.X + q.X) * 0.5f,
                    (p.Y + q.Y) * 0.5f,
                    (p.Z + q.Z) * 0.5f,
                    $"seg{i}"));
            }
        }

        if (includeThickness)
            grips.Add(new ShipGrip(id, ShipGripKind.Thickness, thicknessM, 0f, 0f, $"thick={thicknessM:0.###}"));
        if (!vertexOnly && points.Count >= 2)
            grips.Add(new ShipGrip(id, ShipGripKind.InsertVertex, points[^1].X, points[^1].Y, points[^1].Z, "insert"));
    }
}
