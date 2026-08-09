using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
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
}

public sealed class CadSessionSurfaceTests
{
    [Test]
    public async Task AttachAll_BindsHttpTransport()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-surf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var doc = new CadDocumentSession(settings);
        doc.NewDocument();
        var bus = new CadCommandBus(doc);
        var cad = new CadSessionService(doc, settings, bus, new CadCommandDispatcher(doc, bus, settings));
        var port = 18975 + Random.Shared.Next(0, 100);
        await using var surface = CadSessionSurface.AttachAll(cad, httpPort: port, tcpPort: port + 1);
        await Assert.That(surface).IsNotNull();
        await Assert.That(surface!.Http).IsNotNull();
        await Assert.That(surface.HttpBaseUrl).Contains($":{port}", StringComparison.Ordinal);
    }
}
