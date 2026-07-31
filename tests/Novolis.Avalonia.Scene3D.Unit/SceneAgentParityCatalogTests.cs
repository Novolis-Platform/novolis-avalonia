using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia.Unit.Scene3D;

/// <summary>Agent parity gate for Scene session catalog.</summary>
public sealed class SceneAgentParityCatalogTests
{
    public static readonly string[] SceneProductActions =
    [
        SceneSessionActionIds.New,
        SceneSessionActionIds.Open,
        SceneSessionActionIds.Save,
        SceneSessionActionIds.Select,
        SceneSessionActionIds.Fit,
        SceneSessionActionIds.AddLight,
        SceneSessionActionIds.AddCamera,
        SceneSessionActionIds.AddMesh,
        SceneSessionActionIds.AddMaterial,
        SceneSessionActionIds.SetMeshMaterial,
        SceneSessionActionIds.SetActiveCamera,
        SceneSessionActionIds.MatchViewport,
        SceneSessionActionIds.EnsureStudioLights,
        SceneSessionActionIds.OpenShadeRender,
        SceneSessionActionIds.SaveRenderPng,
        SceneSessionActionIds.DumpViewport,
        SceneSessionActionIds.DescribeScene,
    ];

    [Test]
    public async Task SceneActions_ContainProductAllowlist()
    {
        var session = new SceneSessionService();
        var ids = session.Actions().Actions.Select(a => a.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in SceneProductActions)
            await Assert.That(ids.Contains(required)).IsTrue().Because($"missing Scene action '{required}'");
    }
}
