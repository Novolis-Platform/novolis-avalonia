using System.Numerics;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Services;

/// <summary>PLAN workspace click-to-place / default object creation.</summary>
public static class ShipPlanAuthoring
{
    public static string ToolHint(ShipDesignTool tool) => tool switch
    {
        ShipDesignTool.Select => "Select — click hierarchy or CAD entity",
        ShipDesignTool.Passage => "Passage — click start, then end on the active deck",
        ShipDesignTool.Compartment => "Compartment — click two opposite corners",
        ShipDesignTool.Bulkhead => "Bulkhead — click two path ends (athwartship)",
        ShipDesignTool.Opening => "Opening — click center (host = selection or mid bulkhead)",
        ShipDesignTool.Equipment => "Equipment — click placement center",
        ShipDesignTool.Hull => "Hull — selected",
        ShipDesignTool.Structure => "Structure — selected",
        _ => tool.ToString(),
    };

    /// <summary>Handle a PLAN world click. Returns status text when consumed.</summary>
    public static string? TryHandleWorldClick(ShipDesignSession session, Vector3 world)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.HasShip || session.Workspace != ShipWorkspaceKind.Plan)
            return null;

        return session.ActiveTool switch
        {
            ShipDesignTool.Select => null,
            ShipDesignTool.Hull or ShipDesignTool.Structure => null,
            ShipDesignTool.Passage => PlacePolyline(session, world, need: 2, FinishPassage),
            ShipDesignTool.Compartment => PlacePolyline(session, world, need: 2, FinishCompartment),
            ShipDesignTool.Bulkhead => PlacePolyline(session, world, need: 2, FinishBulkhead),
            ShipDesignTool.Opening => FinishOpening(session, world),
            ShipDesignTool.Equipment => FinishEquipment(session, world),
            _ => null,
        };
    }

    public static string? AddDefault(ShipDesignSession session, ShipDesignTool tool)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.HasShip)
            return "Create a ship first (Create ship panel).";
        if (session.Design.Decks.Count == 0)
            return "Ship has no decks.";

        var deck = ActiveDeck(session);
        var L = session.Design.Ship.LengthMeters;
        var B = session.Design.Ship.BeamMeters;
        var elev = ShipLengths.ToMeters(deck.Elevation);
        var deckH = System.Math.Max(2.2f, session.Design.Ship.HeightMeters / System.Math.Max(1, session.Design.Ship.DeckCount) * 0.9f);

        switch (tool)
        {
            case ShipDesignTool.Passage:
                session.Mutate(d => ShipDesignMutations.AddPassage(
                    d, deck.Id, $"Passage {d.Passages.Count + 1}",
                    [[0f, -L * 0.3f], [0f, L * 0.3f]], 1.2f, 2.2f));
                SelectLastPassage(session);
                return "Added centerline passage on active deck.";
            case ShipDesignTool.Compartment:
                session.Mutate(d => ShipDesignMutations.AddCompartment(
                    d, deck.Id, $"Compartment {d.Compartments.Count + 1}",
                    [[-B * 0.25f, -L * 0.15f], [B * 0.25f, -L * 0.15f], [B * 0.25f, L * 0.15f], [-B * 0.25f, L * 0.15f]]));
                SelectLastCompartment(session);
                return "Added compartment box on active deck.";
            case ShipDesignTool.Bulkhead:
                session.Mutate(d => ShipDesignMutations.AddBulkhead(
                    d, deck.Id, $"Bulkhead {d.Bulkheads.Count + 1}",
                    [[-B * 0.45f, 0f], [B * 0.45f, 0f]],
                    thicknessM: System.Math.Max(0.05f, d.Ship.HullThicknessMeters),
                    heightM: deckH));
                SelectLastBulkhead(session);
                return "Added athwartship bulkhead on active deck.";
            case ShipDesignTool.Opening:
            {
                var host = ResolveOpeningHost(session);
                if (host is null)
                    return "Select a bulkhead (or create one) before placing an opening.";
                session.Mutate(d => ShipDesignMutations.AddOpening(
                    d, host.Value, $"Opening {d.Openings.Count + 1}", OpeningKind.Door,
                    0.9f, 2.0f, [0f, elev + 1f, 0f]));
                SelectLastOpening(session);
                return "Added door opening on host.";
            }
            case ShipDesignTool.Equipment:
                session.Mutate(d => ShipDesignMutations.AddEquipment(
                    d, $"Equipment {d.Equipment.Count + 1}",
                    [0f, elev + 1f, 0f], [1f, 1f, 1.5f], massKg: 800f));
                SelectLastEquipment(session);
                return "Added equipment envelope at origin.";
            default:
                return null;
        }
    }

    private static string? PlacePolyline(
        ShipDesignSession session,
        Vector3 world,
        int need,
        Func<ShipDesignSession, IReadOnlyList<float[]>, string> finish)
    {
        session.AddPlacePoint(world.X, world.Z);
        if (session.PlacePoints.Count < need)
            return $"Point {session.PlacePoints.Count}/{need} — click next.";
        var pts = session.PlacePoints.ToList();
        session.ClearPlacePoints();
        return finish(session, pts);
    }

    private static string FinishPassage(ShipDesignSession session, IReadOnlyList<float[]> pts)
    {
        var deck = ActiveDeck(session);
        session.Mutate(d => ShipDesignMutations.AddPassage(
            d, deck.Id, $"Passage {d.Passages.Count + 1}", pts, 1.2f, 2.2f));
        SelectLastPassage(session);
        return $"Placed passage ({pts.Count} pts).";
    }

    private static string FinishCompartment(ShipDesignSession session, IReadOnlyList<float[]> pts)
    {
        var deck = ActiveDeck(session);
        var a = pts[0];
        var b = pts[1];
        var minX = System.Math.Min(a[0], b[0]);
        var maxX = System.Math.Max(a[0], b[0]);
        var minZ = System.Math.Min(a[1], b[1]);
        var maxZ = System.Math.Max(a[1], b[1]);
        if (maxX - minX < 0.5f)
            maxX = minX + 2f;
        if (maxZ - minZ < 0.5f)
            maxZ = minZ + 2f;
        float[][] poly =
        [
            [minX, minZ],
            [maxX, minZ],
            [maxX, maxZ],
            [minX, maxZ],
        ];
        session.Mutate(d => ShipDesignMutations.AddCompartment(
            d, deck.Id, $"Compartment {d.Compartments.Count + 1}", poly));
        SelectLastCompartment(session);
        return "Placed compartment.";
    }

    private static string FinishBulkhead(ShipDesignSession session, IReadOnlyList<float[]> pts)
    {
        var deck = ActiveDeck(session);
        var deckH = System.Math.Max(2.2f, session.Design.Ship.HeightMeters / System.Math.Max(1, session.Design.Ship.DeckCount) * 0.9f);
        session.Mutate(d => ShipDesignMutations.AddBulkhead(
            d, deck.Id, $"Bulkhead {d.Bulkheads.Count + 1}", pts,
            thicknessM: System.Math.Max(0.05f, d.Ship.HullThicknessMeters),
            heightM: deckH));
        SelectLastBulkhead(session);
        return "Placed bulkhead.";
    }

    private static string FinishOpening(ShipDesignSession session, Vector3 world)
    {
        var host = ResolveOpeningHost(session);
        if (host is null)
            return "Select a bulkhead host first, then click opening center.";
        var deck = ActiveDeck(session);
        var elev = ShipLengths.ToMeters(deck.Elevation);
        session.Mutate(d => ShipDesignMutations.AddOpening(
            d, host.Value, $"Opening {d.Openings.Count + 1}", OpeningKind.Door,
            0.9f, 2.0f, [world.X, elev + 1f, world.Z]));
        SelectLastOpening(session);
        return "Placed opening.";
    }

    private static string FinishEquipment(ShipDesignSession session, Vector3 world)
    {
        var deck = ActiveDeck(session);
        var elev = ShipLengths.ToMeters(deck.Elevation);
        session.Mutate(d => ShipDesignMutations.AddEquipment(
            d, $"Equipment {d.Equipment.Count + 1}",
            [world.X, elev + 1f, world.Z], [1f, 1f, 1.5f], massKg: 800f));
        SelectLastEquipment(session);
        return "Placed equipment.";
    }

    private static DeckDesign ActiveDeck(ShipDesignSession session) =>
        session.Design.Decks[System.Math.Clamp(session.ActiveDeckIndex, 0, session.Design.Decks.Count - 1)];

    private static ShipObjectId? ResolveOpeningHost(ShipDesignSession session)
    {
        if (session.SelectedObjectId is { } sel
            && session.Design.Bulkheads.Any(b => b.Id.Value == sel.Value))
            return sel;
        var deck = ActiveDeck(session);
        var bh = session.Design.Bulkheads.FirstOrDefault(b => b.DeckId?.Value == deck.Id.Value)
                 ?? session.Design.Bulkheads.FirstOrDefault();
        return bh?.Id.AsObject();
    }

    private static void SelectLastPassage(ShipDesignSession session)
    {
        if (session.Design.Passages is { Count: > 0 } p)
            session.Select(p[^1].Id.AsObject());
    }

    private static void SelectLastCompartment(ShipDesignSession session)
    {
        if (session.Design.Compartments is { Count: > 0 } c)
            session.Select(c[^1].Id.AsObject());
    }

    private static void SelectLastBulkhead(ShipDesignSession session)
    {
        if (session.Design.Bulkheads is { Count: > 0 } b)
            session.Select(b[^1].Id.AsObject());
    }

    private static void SelectLastOpening(ShipDesignSession session)
    {
        if (session.Design.Openings is { Count: > 0 } o)
            session.Select(o[^1].Id.AsObject());
    }

    private static void SelectLastEquipment(ShipDesignSession session)
    {
        if (session.Design.Equipment is { Count: > 0 } e)
            session.Select(e[^1].Id.AsObject());
    }
}
