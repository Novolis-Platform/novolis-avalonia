using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;
using Novolis.Ship.Primitives;
using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Avalonia.Ship.Services;

internal static class ShipSessionActions
{
    public static void Register(CadSessionService session)
    {
        session.RegisterAction(ShipChrome.ValidateShipActionId, command =>
        {
            _ = command;
            var doc = session.Document.Document;
            var topo = ShipTopology.Analyze(doc);
            ShipTopology.ApplySpaceFlags(doc, topo);
            var result = ShipValidator.Validate(doc, topo);
            var lines = result.Issues
                .Select(i => $"[{i.Severity}] {i.Code}: {i.Message}")
                .ToList();
            if (lines.Count == 0)
                lines.Add("OK — no ship validation issues.");
            return new CadCommandResultDto
            {
                ActionId = ShipChrome.ValidateShipActionId,
                Ok = result.Ok,
                Message = string.Join('\n', lines),
                ErrorCode = result.Ok ? null : "shipValidationFailed",
            };
        });

        session.RegisterAction(ShipChrome.RefreshAirtightActionId, command =>
        {
            _ = command;
            var doc = session.Document.Document;
            var topo = ShipTopology.Analyze(doc);
            ShipTopology.ApplySpaceFlags(doc, topo);
            return new CadCommandResultDto
            {
                ActionId = ShipChrome.RefreshAirtightActionId,
                Ok = true,
                Message =
                    $"Airtight: {topo.SealedComponents.Count} sealed component(s), {topo.VentingToExterior.Count} venting space(s), {topo.OrphanOpeningIds.Count} orphan opening(s).",
            };
        });

        session.RegisterAction(ShipChrome.PlaceHatchActionId, command =>
        {
            if (!TryGuidProp(command, "hostWallId", out var wallId))
                return Fail(ShipChrome.PlaceHatchActionId, "Need hostWallId.", "badArgs");

            var doc = session.Document.Document;
            var wall = doc.Entities.FirstOrDefault(e => e.Id == wallId);
            if (wall is null)
                return Fail(ShipChrome.PlaceHatchActionId, "Host wall not found.", "notFound");

            var clearW = PropFloat(command, "clearWidth", 1.0f);
            var clearH = PropFloat(command, "clearHeight", 2.2f);
            var mid = WallMid(wall);
            var opening = new CadEntity
            {
                Kind = "opening",
                Name = Prop(command, "name") ?? "Hatch",
                OpeningType = Prop(command, "openingType") ?? "hatch",
                Deck = wall.Deck,
                HostWallId = wallId,
                Height = clearH,
                Footprint =
                [
                    [mid.X - clearW * 0.5f, mid.Y, mid.Z - 0.05f],
                    [mid.X + clearW * 0.5f, mid.Y, mid.Z - 0.05f],
                    [mid.X + clearW * 0.5f, mid.Y, mid.Z + 0.05f],
                    [mid.X - clearW * 0.5f, mid.Y, mid.Z + 0.05f],
                ],
            };
            ShipCad.TagOpeningPressure(
                opening,
                ShipPressureClass.Habitable,
                clearW,
                clearH,
                airtightWhenClosed: true,
                leafState: ShipLeafState.Closed);

            session.Bus.Execute(new AddEntityCommand(opening));
            OpeningDerivation.Apply(doc.Entities);
            return new CadCommandResultDto
            {
                ActionId = ShipChrome.PlaceHatchActionId,
                Ok = true,
                Message = $"Placed hatch {opening.Id}",
                Paths = [opening.Id.ToString()],
            };
        });
    }

    private static CadCommandResultDto Fail(string id, string message, string code) =>
        new() { ActionId = id, Ok = false, Message = message, ErrorCode = code };

    private static string? Prop(CadCommandDto command, string key)
    {
        if (command.Properties is null || !command.Properties.TryGetValue(key, out var v))
            return null;
        return v;
    }

    private static float PropFloat(CadCommandDto command, string key, float fallback)
    {
        var raw = Prop(command, key);
        return float.TryParse(raw, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : fallback;
    }

    private static bool TryGuidProp(CadCommandDto command, string key, out Guid id)
    {
        id = default;
        var raw = Prop(command, key);
        return raw is not null && Guid.TryParse(raw, out id);
    }

    private static System.Numerics.Vector3 WallMid(CadEntity wall)
    {
        if (wall.A is { Length: >= 3 } && wall.B is { Length: >= 3 })
        {
            var a = CadVec.To(wall.A);
            var b = CadVec.To(wall.B);
            return (a + b) * 0.5f;
        }

        return System.Numerics.Vector3.Zero;
    }
}
