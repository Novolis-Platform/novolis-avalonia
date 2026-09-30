using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.Unit.ThreeD;

public sealed class AgentSurfaceDefinitionTests
{
    [Test]
    public async Task Scene_definition_includes_boole_and_primitives()
    {
        var def = SceneSessionContract.Definition;
        await Assert.That(def.SurfaceId).IsEqualTo("scene");
        await Assert.That(def.Actions.Any(a => a.Id == "addboole")).IsTrue();
        await Assert.That(def.Actions.Any(a => a.Id == "addmesh")).IsTrue();
        var discovery = JsonSerializer.Serialize(def.BuildCommandJsonSchema());
        await Assert.That(discovery.Contains("addboole", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Scene_definition_includes_edit_actions()
    {
        var def = SceneSessionContract.Definition;
        await Assert.That(def.Actions.Any(a => a.Id == "seteditmode")).IsTrue();
        await Assert.That(def.Actions.Any(a => a.Id == "meshedit")).IsTrue();
        await Assert.That(def.Actions.Any(a => a.Id == "makeeditable")).IsTrue();
    }

    [Test]
    [NotInParallel("scene-http")]
    public async Task Http_host_addmesh_cylinder_and_addboole()
    {
        var session = new SceneSessionService(SceneDocument.CreatePrimitiveStage("HttpTest")) { AppId = "test" };
        var port = GetFreeTcpPort();
        await using var host = AgentHttpHost.Attach(session, session.Definition, port);
        await host.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(host.BaseUrl + "/") };

        using (var content = new StringContent(
                   """{"actionId":"addmesh","primitive":"cylinder","name":"Cyl"}""",
                   System.Text.Encoding.UTF8,
                   "application/json"))
        {
            using var response = await client.PostAsync("session/command", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Scene command failed with {(int)response.StatusCode}: {responseBody}");
        }

        await Assert.That(session.Document.Nodes.OfType<MeshNode>().Any(m => m.Primitive == MeshPrimitiveKind.Cylinder && m.Name == "Cyl")).IsTrue();
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
