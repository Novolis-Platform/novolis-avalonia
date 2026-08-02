using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadSessionServiceExtendedTests
{
    private static (CadDocumentSession Doc, CadEditorSettings Settings, CadSessionService Cad) Create(out string root)
    {
        root = Path.Combine(Path.GetTempPath(), "novolis-cad-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var doc = new CadDocumentSession(settings);
        doc.NewDocument();
        var bus = new CadCommandBus(doc);
        var dispatcher = new CadCommandDispatcher(doc, bus, settings);
        var cad = new CadSessionService(doc, settings, bus, dispatcher);
        return (doc, settings, cad);
    }

    [Test]
    public async Task Hello_And_Actions_ReturnCatalog()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            var hello = cad.Hello();
            await Assert.That(hello.AppId).IsEqualTo("novolis.cad");
            await Assert.That(hello.ProcessId).IsGreaterThan(0);

            var actions = cad.Actions().Actions;
            await Assert.That(actions.Any(a => a.Id == CadSessionActionIds.Save)).IsTrue();
            await Assert.That(actions.First(a => a.Id == CadSessionActionIds.Undo).Enabled).IsFalse();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task SettingsCommands_UpdateSnapshot()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetElevation, Elevation = 2.5f }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetSnap, Snap = false }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetGrid, GridStep = 0.25f }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetAxisLock, Kind = "x" }).Ok).IsTrue();

            var snap = cad.Snapshot();
            await Assert.That(snap.DrawElevation).IsEqualTo(2.5f);
            await Assert.That(snap.SnapToGrid).IsFalse();
            await Assert.That(snap.GridStep).IsEqualTo(0.25f);
            await Assert.That(snap.AxisLock).IsEqualTo("x");
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task SettingsCommands_RejectBadArgs()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            var elev = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetElevation });
            await Assert.That(elev.Ok).IsFalse();
            await Assert.That(elev.ErrorCode).IsEqualTo("badElevation");

            var grid = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetGrid, GridStep = 0f });
            await Assert.That(grid.Ok).IsFalse();

            var tool = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetTool, Tool = "bogus" });
            await Assert.That(tool.Ok).IsFalse();
            await Assert.That(tool.ErrorCode).IsEqualTo("badTool");
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task SaveOpen_New_UndoRedo_DeleteSelection()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            var path = Path.Combine(root, "saved.cadjson");
            cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.New });
            var before = doc.Document.Entities.Count;

            var box = new CadEntity { Kind = "box", Center = [0f, 0f, 0f], HalfExtents = [0.5f, 0.5f, 0.5f] };
            cad.Bus.Execute(new AddEntityCommand(box));
            doc.SelectedId = box.Id;

            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Save, Path = path }).Ok).IsTrue();
            await Assert.That(File.Exists(path)).IsTrue();

            cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.DeleteSelection });
            await Assert.That(doc.Document.Entities.Any(e => e.Id == box.Id)).IsFalse();

            cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Undo });
            await Assert.That(doc.Document.Entities.Any(e => e.Id == box.Id)).IsTrue();

            cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Open, Path = path });
            await Assert.That(doc.Document.Entities.Count).IsGreaterThanOrEqualTo(before);
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task DraftingActions_AddEntitiesViaSession()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            var addedLine = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.AddLine,
                Properties = new Dictionary<string, string>
                {
                    ["a"] = "0,0,0",
                    ["b"] = "1,0,0",
                },
            });
            await Assert.That(addedLine.Ok).IsTrue();

            var addedCircle = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.AddCircle,
                Center = [2f, 0f, 0f],
                Properties = new Dictionary<string, string> { ["radius"] = "0.5" },
            });
            await Assert.That(addedCircle.Ok).IsTrue();

            var addedBox = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.AddBox,
                Center = [0f, 0.5f, 0f],
                Spacing = [0.5f, 0.5f, 0.5f],
            });
            await Assert.That(addedBox.Ok).IsTrue();

            await Assert.That(doc.Document.Entities.Count(e => e.Kind == "line")).IsGreaterThanOrEqualTo(2);
            await Assert.That(doc.Document.Entities.Count(e => e.Kind == "circle")).IsGreaterThanOrEqualTo(2);
            await Assert.That(doc.Document.Entities.Any(e => e.Kind == "box")).IsTrue();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task SetMaterial_And_SetWallSide_MutateEntity()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            var wall = new CadEntity
            {
                Kind = "wall",
                A = [0f, 0f, 0f],
                B = [2f, 0f, 0f],
                Height = 2.4f,
            };
            cad.Bus.Execute(new AddEntityCommand(wall));
            doc.SelectedId = wall.Id;

            await Assert.That(cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.SetMaterial,
                Properties = new Dictionary<string, string> { ["material"] = "Concrete" },
            }).Ok).IsTrue();
            await Assert.That(wall.Material).IsEqualTo("Concrete");

            var shapeId = Guid.NewGuid();
            await Assert.That(cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.SetWallSide,
                Properties = new Dictionary<string, string>
                {
                    ["side"] = "A",
                    ["shapeId"] = shapeId.ToString(),
                },
            }).Ok).IsTrue();
            await Assert.That(wall.Sides?.A?.ShapeId).IsEqualTo(shapeId);
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task BridgeScene_And_ExportScene()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            SceneDocument? bridged = null;
            cad.SceneBridged += s => bridged = s;
            var bridge = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.BridgeScene });
            await Assert.That(bridge.Ok).IsTrue();
            await Assert.That(bridged).IsNotNull();
            await Assert.That(cad.LastBridgedScene).IsNotNull();

            var exportPath = Path.Combine(root, "scene.nov3djson");
            var export = cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.ExportScene,
                Path = exportPath,
            });
            await Assert.That(export.Ok).IsTrue();
            await Assert.That(File.Exists(exportPath)).IsTrue();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task ExportPhys_WritesFile()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            var path = Path.Combine(root, "out.cadphys.json");
            var result = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.ExportPhys, Path = path });
            await Assert.That(result.Ok).IsTrue();
            await Assert.That(File.Exists(path)).IsTrue();
            await Assert.That(cad.Snapshot().RecentExportPaths).Contains(path);
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task RegisterAction_And_StudioWorkspace()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            cad.RegisterAction("customPing", _ => new CadCommandResultDto
            {
                Ok = true,
                ActionId = "customPing",
                Message = "pong",
            });
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = "customPing" }).Message).IsEqualTo("pong");

            string? requested = null;
            cad.StudioWorkspaceRequested += w => requested = w;
            await Assert.That(cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.SetStudioWorkspace,
                Workspace = "draft3d",
            }).Ok).IsTrue();
            await Assert.That(requested).IsEqualTo("draft3d");
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task ModelingActions_SplitConnectGroupWeldOptimizeBridge()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            var box = new CadEntity { Kind = "box", Center = [0f, 0f, 0f], HalfExtents = [0.5f, 0.5f, 0.5f] };
            cad.Bus.Execute(new AddEntityCommand(box));
            doc.SelectedId = box.Id;

            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Split, SourceId = box.Id }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Group, MemberIds = [box.Id] }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Connect, MemberIds = [box.Id], Mode = "group" }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.AddMaterial }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.AddCamera }).Ok).IsTrue();

            var mesh = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.MeshFromSolid, SourceId = box.Id });
            await Assert.That(mesh.Ok).IsTrue();
            var meshEntity = doc.Document.Entities.First(e => e.Kind == "meshFromSolid");
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Weld, SourceId = meshEntity.Id }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Optimize, SourceId = meshEntity.Id }).Ok).IsTrue();
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Bridge, SourceId = meshEntity.Id }).Ok).IsTrue();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task DraftingActions_WallSplineExtrudeWallAndFailures()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            await Assert.That(cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.AddWall,
                Properties = new Dictionary<string, string>
                {
                    ["a"] = "0,0,0",
                    ["b"] = "4,0,0",
                    ["height"] = "3",
                },
            }).Ok).IsTrue();

            await Assert.That(cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.AddSpline,
                Properties = new Dictionary<string, string>
                {
                    ["points"] = "0,0,0;1,0,1;2,0,0",
                },
            }).Ok).IsTrue();

            await Assert.That(cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.ExtrudeProfile,
                Properties = new Dictionary<string, string>
                {
                    ["points"] = "0,0,0;2,0,0;2,0,2;0,0,2",
                    ["height"] = "1.5",
                },
            }).Ok).IsTrue();

            var badWall = cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.AddWall });
            await Assert.That(badWall.Ok).IsFalse();
            await Assert.That(badWall.ErrorCode).IsEqualTo("badArgs");

            await Assert.That(doc.Document.Entities.Any(e => e.Kind == "wall")).IsTrue();
            await Assert.That(doc.Document.Entities.Any(e => e.Kind == "spline")).IsTrue();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task Select_Additive_And_FitHandler()
    {
        var (doc, _, cad) = Create(out var root);
        try
        {
            var ids = doc.Document.Entities.Take(2).Select(e => e.Id).ToArray();
            cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Select, EntityId = ids[0] });
            cad.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.Select,
                EntityId = ids[1],
                Properties = new Dictionary<string, string> { ["additive"] = "true" },
            });
            await Assert.That(doc.SelectedIds.Count).IsEqualTo(2);

            var fitCalled = false;
            cad.FitHandler = () => fitCalled = true;
            await Assert.That(cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.Fit }).Ok).IsTrue();
            await Assert.That(fitCalled).IsTrue();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task Subscribe_RaisesChangedEvent()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            cad.Subscribe();
            var reasons = new List<string>();
            cad.Changed += e => reasons.Add(e.Reason);
            cad.Execute(new CadCommandDto { ActionId = CadSessionActionIds.SetTool, Tool = "rect" });
            await Assert.That(reasons.Any(r => r == "command")).IsTrue();
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }

    [Test]
    public async Task UnknownAction_ReturnsError()
    {
        var (_, _, cad) = Create(out var root);
        try
        {
            var result = cad.Execute(new CadCommandDto { ActionId = "notReal" });
            await Assert.That(result.Ok).IsFalse();
            await Assert.That(result.ErrorCode).IsEqualTo("unknownAction");
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* ignore */ } }
    }
}
