using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia.Unit.Scene3D;

public sealed class ModelingSceneTests
{
    [Test]
    public async Task Empty_document_round_trips_json()
    {
        var doc = SceneDocument.CreateEmpty("RoundTrip");
        var json = SceneSerializer.Serialize(doc);
        var again = SceneSerializer.Deserialize(json);
        await Assert.That(again.Name).IsEqualTo("RoundTrip");
        await Assert.That(again.Nodes.OfType<LightNode>().Any()).IsTrue();
        await Assert.That(again.Format).IsEqualTo("novolis.scene");
    }

    [Test]
    public async Task Light_change_invalidates_look_only()
    {
        var doc = SceneDocument.CreateEmpty();
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var before = eval.Cache;
        var meshGen = before.MeshGeneration;
        var lookGen = before.LookGeneration;

        var light = doc.Nodes.OfType<LightNode>().First();
        light.Intensity = 9f;
        eval.NotifyNodeChanged(light);
        var after = eval.Cache;

        await Assert.That(after.MeshGeneration).IsEqualTo(meshGen);
        await Assert.That(after.LookGeneration).IsGreaterThan(lookGen);
        await Assert.That(after.Lights.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Rendering_light_export_maps_infinite_and_omni()
    {
        var doc = SceneDocument.CreateSpotRimSample();
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var exported = RenderingLightExport.Export(eval.Cache);
        await Assert.That(exported.Any(e => e.Kind == "Directional")).IsTrue();
        await Assert.That(exported.Any(e => e.Kind == "Point")).IsTrue();
    }

    [Test]
    public async Task Mesh_stack_cloner_expands()
    {
        var doc = SceneDocument.CreateEmpty();
        var mesh = doc.Nodes.OfType<MeshNode>().First(m => m.Name == "Box");
        doc.Nodes.Add(new GeneratorNode
        {
            Name = "Cloner",
            ParentId = doc.Roots().First().Id,
            Generator = GeneratorKind.Cloner,
            SourceId = mesh.Id,
            Count = 3,
            Offset = [1.5f, 0, 0],
        });
        var eval = new SceneEvaluator();
        eval.Bind(doc);
        var derived = MeshStackEvaluator.Expand(doc, eval.Cache);
        await Assert.That(derived.Count).IsEqualTo(3);
    }
}

public sealed class AgentSurfaceDefinitionTests
{
    [Test]
    public async Task Scene_definition_emits_actions_openapi_and_mcp()
    {
        var def = SceneSessionContract.Definition;
        await Assert.That(def.SurfaceId).IsEqualTo("scene");
        await Assert.That(def.DefaultHttpPort).IsEqualTo(18785);
        await Assert.That(def.Actions.Any(a => a.Id == "addlight")).IsTrue();

        var openApi = def.BuildOpenApiFragment();
        await Assert.That(openApi["openapi"]).IsEqualTo("3.0.3");

        var mcp = def.BuildMcpTools("scene");
        await Assert.That(mcp.Any(t => t.Name == "scene_command")).IsTrue();

        var discovery = def.ToDiscoveryJson();
        await Assert.That(discovery.Contains("addlight", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Http_host_serves_hello_and_addlight()
    {
        var session = new SceneSessionService(SceneDocument.CreateEmpty("HttpTest")) { AppId = "test" };
        var port = 18885 + Random.Shared.Next(0, 200);
        await using var host = AgentHttpHost.Attach(session, session.Definition, port);
        using var client = new HttpClient { BaseAddress = new Uri(host.BaseUrl + "/") };

        var helloJson = await client.GetStringAsync("session/hello");
        await Assert.That(helloJson.Contains("scene", StringComparison.Ordinal)).IsTrue();

        using var content = new StringContent(
            """{"actionId":"addlight","lightKind":"spot","intensity":3}""",
            System.Text.Encoding.UTF8,
            "application/json");
        using var response = await client.PostAsync("session/command", content);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body.Contains("\"ok\":true", StringComparison.Ordinal)).IsTrue();
        await Assert.That(session.Document.Nodes.OfType<LightNode>().Any(l => l.LightKind == LightKind.Spot)).IsTrue();
    }
}
