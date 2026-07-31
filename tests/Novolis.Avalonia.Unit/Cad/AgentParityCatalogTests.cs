using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;

namespace Novolis.Avalonia.Unit.Cad;

/// <summary>Agent parity gate for Cad session catalog + drafting smoke.</summary>
public sealed class AgentParityCatalogTests
{
    public static readonly string[] CadProductActions =
    [
        CadSessionActionIds.New,
        CadSessionActionIds.Open,
        CadSessionActionIds.Save,
        CadSessionActionIds.Undo,
        CadSessionActionIds.Redo,
        CadSessionActionIds.DeleteSelection,
        CadSessionActionIds.Select,
        CadSessionActionIds.Fit,
        CadSessionActionIds.SetTool,
        CadSessionActionIds.SetWorkspace,
        CadSessionActionIds.SetStudioWorkspace,
        CadSessionActionIds.ExportPlanPng,
        CadSessionActionIds.ExportScene,
        CadSessionActionIds.BridgeScene,
        CadSessionActionIds.SetMaterial,
        CadSessionActionIds.SetWallSide,
        CadSessionActionIds.AddWall,
        CadSessionActionIds.ExtrudeProfile,
        CadSessionActionIds.AddDimension,
        CadSessionActionIds.AddLine,
        CadSessionActionIds.AddCircle,
        CadSessionActionIds.AddRect,
        CadSessionActionIds.AddSpline,
        CadSessionActionIds.AddBox,
        CadSessionActionIds.SetSnap,
        CadSessionActionIds.SetGrid,
        CadSessionActionIds.SetAxisLock,
        CadSessionActionIds.AddMaterial,
        CadSessionActionIds.AddLight,
        CadSessionActionIds.AddCamera,
    ];

    [Test]
    public async Task CadActions_ContainProductAllowlist()
    {
        var settings = new CadEditorSettings(Path.Combine(Path.GetTempPath(), "novolis-parity-cad"));
        var document = new CadDocumentSession(settings);
        var bus = new CadCommandBus(document);
        var dispatcher = new CadCommandDispatcher(document, bus, settings);
        var session = new CadSessionService(document, settings, bus, dispatcher);
        var ids = session.Actions().Actions.Select(a => a.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in CadProductActions)
            await Assert.That(ids.Contains(required)).IsTrue().Because($"missing Cad action '{required}'");
    }

    [Test]
    public async Task Cad_AddRect_Extrude_SetMaterial_ExportScene_Smoke()
    {
        var settings = new CadEditorSettings(Path.Combine(Path.GetTempPath(), "novolis-parity-smoke"));
        var document = new CadDocumentSession(settings);
        var bus = new CadCommandBus(document);
        var dispatcher = new CadCommandDispatcher(document, bus, settings);
        var session = new CadSessionService(document, settings, bus, dispatcher);

        var rect = session.Execute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.AddRect,
            Properties = new Dictionary<string, string>
            {
                ["a"] = "0,0,0",
                ["b"] = "4,0,3",
            },
        });
        await Assert.That(rect.Ok).IsTrue();

        var extrude = session.Execute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.ExtrudeProfile,
            Properties = new Dictionary<string, string>
            {
                ["points"] = "0,0,0;4,0,0;4,0,3;0,0,3",
                ["height"] = "2.4",
            },
        });
        await Assert.That(extrude.Ok).IsTrue();

        var mat = session.Execute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.SetMaterial,
            Kind = "Concrete",
        });
        await Assert.That(mat.Ok).IsTrue();

        var path = Path.Combine(Path.GetTempPath(), $"parity-{Guid.NewGuid():N}.nov3djson");
        try
        {
            var export = session.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.ExportScene,
                Path = path,
            });
            await Assert.That(export.Ok).IsTrue();
            await Assert.That(File.Exists(path)).IsTrue();
            await Assert.That(new FileInfo(path).Length).IsGreaterThan(100);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
