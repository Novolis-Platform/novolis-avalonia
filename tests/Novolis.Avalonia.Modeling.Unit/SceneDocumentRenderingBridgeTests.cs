using Novolis.Avalonia.Modeling;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Rendering.Materials;
using Novolis.Rendering.Scene;
using Novolis.Modeling;
using RenderLightKind = Novolis.Rendering.Scene.LightKind;

namespace Novolis.Avalonia.Unit.Modeling;

public sealed class SceneDocumentRenderingBridgeTests
{
    [Test]
    public async Task PrimitiveStage_ToScene_HasMeshesAndStandardMaterials()
    {
        var scene = SceneDocumentRenderingBridge.ToScene(SceneDocument.CreatePrimitiveStage());

        await Assert.That(scene.Meshes.Count).IsGreaterThan(0);
        await Assert.That(scene.Meshes.All(m => m.Material is StandardMaterial)).IsTrue();
        await Assert.That(scene.Lights.Any(l => l.Kind == RenderLightKind.Point)).IsTrue();
    }

    [Test]
    public async Task LookSetup_Maps_Infinite_To_Directional_And_Others_To_Point()
    {
        var scene = SceneDocumentRenderingBridge.ToScene(SceneDocument.CreateLookSetup());

        await Assert.That(scene.Lights.Any(l => l.Kind == RenderLightKind.Directional)).IsTrue();
        await Assert.That(scene.Lights.Any(l => l.Kind == RenderLightKind.Point)).IsTrue();
        await Assert.That(scene.Meshes.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Disabled_lights_are_omitted()
    {
        var doc = SceneDocument.CreateLookSetup();
        foreach (var light in doc.Nodes.OfType<LightNode>())
            light.Enabled = false;

        var scene = SceneDocumentRenderingBridge.ToScene(doc);
        await Assert.That(scene.Lights.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Session_ToTraceScene_Matches_Bridge()
    {
        var session = new SceneSessionService(SceneDocument.CreatePrimitiveStage());
        var fromSession = session.ToTraceScene();
        var fromBridge = SceneDocumentRenderingBridge.ToScene(session.Evaluator.Cache);

        await Assert.That(fromSession.Meshes.Count).IsEqualTo(fromBridge.Meshes.Count);
        await Assert.That(fromSession.Lights.Count).IsEqualTo(fromBridge.Lights.Count);
    }
}
