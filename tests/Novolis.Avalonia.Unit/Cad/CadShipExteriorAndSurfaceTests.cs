using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadShipExteriorTests
{
    [Test]
    public async Task ShouldUseExterior_DetectsShipDocument()
    {
        var shipLike = new CadDocument();
        shipLike.Entities.Add(new CadEntity { Kind = "wall", Name = "Hull" });
        shipLike.Entities.Add(new CadEntity { Kind = "space", Name = "Hold" });
        await Assert.That(CadShipExterior.ShouldUseExterior(shipLike)).IsTrue();

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
