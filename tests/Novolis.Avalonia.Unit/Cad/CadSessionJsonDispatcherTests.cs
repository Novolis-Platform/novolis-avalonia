using System.Text.Json;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadSessionJsonDispatcherTests
{
    private static CadSessionService CreateService()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-json-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var session = new CadDocumentSession(settings);
        session.NewDocument();
        var bus = new CadCommandBus(session);
        var dispatcher = new CadCommandDispatcher(session, bus, settings);
        return new CadSessionService(session, settings, bus, dispatcher);
    }

    [Test]
    public async Task Dispatch_Hello_ReturnsProcessId()
    {
        var cad = CreateService();
        var result = CadSessionJsonDispatcher.Dispatch(cad, CadSessionMethodNames.Hello, default);
        var hello = (CadHelloResponseDto)result;
        await Assert.That(hello.ProcessId).IsEqualTo(Environment.ProcessId);
        await Assert.That(hello.Capabilities).Contains("snapshot");
    }

    [Test]
    public async Task Dispatch_Subscribe_EnablesEvents()
    {
        var cad = CreateService();
        cad.Subscribe();
        var fired = false;
        cad.ActionResult += _ => fired = true;
        var sub = (CadSubscribeResponseDto)CadSessionJsonDispatcher.Dispatch(cad, "subscribe", default);
        await Assert.That(sub.Ok).IsTrue();

        cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetTool, Tool = "select" });
        await Assert.That(fired).IsTrue();
    }

    [Test]
    public async Task ParseCommand_ReadsNestedArgs()
    {
        using var doc = JsonDocument.Parse("""
            {
              "command": {
                "actionId": "setTool",
                "tool": "line",
                "entityId": "a0000000-0000-4000-8000-000000000001",
                "elevation": 1.5,
                "snap": true,
                "mergeAtPlane": false
              }
            }
            """);
        var cmd = CadSessionJsonDispatcher.ParseCommand(doc.RootElement);
        await Assert.That(cmd.ActionId).IsEqualTo("setTool");
        await Assert.That(cmd.Tool).IsEqualTo("line");
        await Assert.That(cmd.EntityId).IsEqualTo(Guid.Parse("a0000000-0000-4000-8000-000000000001"));
        await Assert.That(cmd.Elevation).IsEqualTo(1.5f);
        await Assert.That(cmd.Snap).IsTrue();
        await Assert.That(cmd.MergeAtPlane).IsFalse();
    }

    [Test]
    public async Task Dispatch_UnknownMethod_Throws()
    {
        var cad = CreateService();
        await Assert.That(() => CadSessionJsonDispatcher.Dispatch(cad, "nope", default))
            .Throws<InvalidOperationException>();
    }
}
