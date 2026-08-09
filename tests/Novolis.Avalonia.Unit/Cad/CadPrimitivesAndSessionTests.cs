using System.Text.Json;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Ship.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;
using Novolis.Cad.Evaluation;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadPrimitivesAndSessionTests
{
    [Test]
    public async Task CadDocument_RoundTripsJson()
    {
        var doc = CadDocumentSession.CreateStarter();
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });
        var loaded = JsonSerializer.Deserialize<CadDocument>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        });
        await Assert.That(loaded).IsNotNull();
        await Assert.That(loaded!.Format).IsEqualTo("novolis.cad");
        await Assert.That(loaded.Entities.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CadSessionService_SetTool_And_Snapshot()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-unit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.NewDocument();
            var bus = new CadCommandBus(session);
            var dispatcher = new CadCommandDispatcher(session, bus, settings);
            var cad = new CadSessionService(session, settings, bus, dispatcher);
            cad.Subscribe();

            var result = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetTool, Tool = "line" });
            await Assert.That(result.Ok).IsTrue();
            var snap = cad.Snapshot();
            await Assert.That(snap.ActiveTool).IsEqualTo("line");
            await Assert.That(snap.EntityCount).IsGreaterThan(0);
            await Assert.That(cad.Actions().Actions.Any(a => a.Id == CadSessionActionIds.Save)).IsTrue();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task CadSessionHttpHost_SnapshotAndCommand()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-http-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.NewDocument();
            var bus = new CadCommandBus(session);
            var dispatcher = new CadCommandDispatcher(session, bus, settings);
            var cad = new CadSessionService(session, settings, bus, dispatcher);
            var port = 18875 + Random.Shared.Next(0, 200);
            await using var host = CadSessionHttpHost.Attach(cad, port);
            using var client = new HttpClient { BaseAddress = new Uri(host.BaseUrl + "/") };
            var snapJson = await client.GetStringAsync("session/snapshot");
            await Assert.That(snapJson.Contains("entityCount", StringComparison.Ordinal)).IsTrue();

            using var content = new StringContent(
                """{"actionId":"setTool","tool":"circle"}""",
                System.Text.Encoding.UTF8,
                "application/json");
            using var response = await client.PostAsync("session/command", content);
            var body = await response.Content.ReadAsStringAsync();
            await Assert.That(response.IsSuccessStatusCode).IsTrue();
            await Assert.That(body.Contains("\"ok\":true", StringComparison.Ordinal)).IsTrue();
            await Assert.That(cad.Dispatcher.ActiveTool).IsEqualTo(CadToolKind.Circle);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task CadShipImport_LoadsShipEntitiesAndBounds()
    {
        var src = CadShipImport.ResolveSourceCadjson();
        if (src is null)
            return; // no generated ship on this machine

        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-ship-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var imported = CadShipImport.ImportIntoWorkspace(root, src);
            var settings = new CadEditorSettings(root, CadShipImport.WorkspaceFolderName);
            var session = new CadDocumentSession(settings);
            session.OpenFromPath(imported);
            await Assert.That(session.Document.Entities.Count).IsGreaterThan(100);
            await Assert.That(session.Document.Entities.Any(e => e.Kind == "wall")).IsTrue();
            await Assert.That(session.Document.Entities.Any(e => e.Kind == "space")).IsTrue();
            await Assert.That(session.Document.Entities.Count(e => e.Kind == "box" && CadShipGeometry.TryGetBox(e, out _, out _)))
                .IsGreaterThan(0);
            var (_, radius) = EntityBounds.Compute(session.Document);
            await Assert.That(radius).IsGreaterThan(10f);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
