using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Cad.Ship;
using Novolis.Avalonia.Cad.Ship.Services;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadShipExteriorTests
{
    [Test]
    public async Task ShouldUseExterior_DetectsShipDocumentWithAuthoredSolids()
    {
        var decksOnly = new CadDocument();
        for (var i = 0; i < 8; i++)
            decksOnly.Entities.Add(new CadEntity { Kind = i % 2 == 0 ? "wall" : "space", Name = $"Deck{i}" });
        await Assert.That(CadShipExterior.ShouldUseExterior(decksOnly)).IsFalse();

        var withExterior = new CadDocument();
        for (var i = 0; i < 8; i++)
            withExterior.Entities.Add(new CadEntity { Kind = i % 2 == 0 ? "wall" : "space", Name = $"Deck{i}" });
        withExterior.Entities.Add(new CadEntity
        {
            Kind = "box",
            Name = "ext-hull",
            Center = [0f, 1f, 0f],
            HalfExtents = [1f, 1f, 1f],
            Properties = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["exterior"] = System.Text.Json.JsonSerializer.SerializeToElement(true),
            },
        });
        await Assert.That(CadShipExterior.ShouldUseExterior(withExterior)).IsTrue();

        var plain = CadDocumentSession.CreateStarter();
        await Assert.That(CadShipExterior.ShouldUseExterior(plain)).IsFalse();
    }

    [Test]
    public async Task ShipChromeAttachmentIsScopedToOneCadSession()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "novolis-cad-ship-hooks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var document = new CadDocumentSession(settings);
            var bus = new CadCommandBus(document);
            var dispatcher = new CadCommandDispatcher(document, bus, settings);
            var cad = new CadSessionService(document, settings, bus, dispatcher);

            using (var attachment = CadShipChrome.Attach(cad))
            {
                await Assert.That(cad.ExteriorHooks).IsNotNull();
                await Assert.That(cad.Actions().Actions.Any(
                    action => action.Id == CadShipChrome.ImportShipActionId)).IsTrue();
            }

            await Assert.That(cad.ExteriorHooks).IsNull();
            await Assert.That(cad.Actions().Actions.Any(
                action => action.Id == CadShipChrome.ImportShipActionId)).IsFalse();
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }
}
