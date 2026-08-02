using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadCommandDispatcherExtendedTests
{
    private static (CadDocumentSession Doc, CadCommandDispatcher Dispatcher) Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-disp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var doc = new CadDocumentSession(settings);
        doc.NewDocument();
        var bus = new CadCommandBus(doc);
        return (doc, new CadCommandDispatcher(doc, bus, settings));
    }

    [Test]
    public async Task TryDispatch_WallSplineDimension_BoxSphereCylinder()
    {
        var (doc, dispatcher) = Create();
        await Assert.That(dispatcher.TryDispatch("Wall(Point(0,0), Point(4,0), 0.2, 2.5)")).IsNull();
        await Assert.That(doc.Document.Entities.Any(e => e.Kind == "wall")).IsTrue();

        await Assert.That(dispatcher.TryDispatch("Spline(Point(0,0), Point(1,0), Point(1,1))")).IsNull();
        await Assert.That(doc.Document.Entities.Any(e => e.Kind == "spline")).IsTrue();

        await Assert.That(dispatcher.TryDispatch("Dim(Point(0,0), Point(3,0), 0.5)")).IsNull();
        await Assert.That(doc.Document.Entities.Any(e => e.Kind == "dimension")).IsTrue();

        await Assert.That(dispatcher.TryDispatch("Box(2,1,1)")).IsNull();
        await Assert.That(dispatcher.TryDispatch("Sphere(0.5)")).IsNull();
        await Assert.That(dispatcher.TryDispatch("Cylinder(0.4, 2)")).IsNull();
        await Assert.That(doc.Document.Entities.Count(e => e.Kind is "box" or "sphere" or "cylinder")).IsEqualTo(3);
    }

    [Test]
    public async Task TryDispatch_UndoRedo_Delete_FitEvents()
    {
        var (doc, dispatcher) = Create();
        dispatcher.TryDispatch("Line(Point(0,0), Point(1,0))");
        var count = doc.Document.Entities.Count;

        var fitFired = false;
        dispatcher.FitRequested += () => fitFired = true;
        await Assert.That(dispatcher.TryDispatch("Fit")).IsNull();
        await Assert.That(fitFired).IsTrue();

        doc.SelectedId = doc.Document.Entities.Last().Id;
        await Assert.That(dispatcher.TryDispatch("Delete")).IsNull();
        await Assert.That(doc.Document.Entities.Count).IsLessThan(count);

        await Assert.That(dispatcher.TryDispatch("Undo")).IsNull();
        await Assert.That(doc.Document.Entities.Count).IsEqualTo(count);

        await Assert.That(dispatcher.TryDispatch("Redo")).IsNull();
        await Assert.That(doc.Document.Entities.Count).IsLessThan(count);
    }

    [Test]
    public async Task TryDispatch_SnapLevelAxisWorkspaceViaSession()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-disp2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var doc = new CadDocumentSession(settings);
        doc.NewDocument();
        var bus = new CadCommandBus(doc);
        var dispatcher = new CadCommandDispatcher(doc, bus, settings);
        var cad = new CadSessionService(doc, settings, bus, dispatcher);
        dispatcher.SessionExecute = cad.Execute;

        await Assert.That(dispatcher.TryDispatch("Snap(true)")).IsNull();
        await Assert.That(dispatcher.TryDispatch("Level(1.25)")).IsNull();
        await Assert.That(dispatcher.TryDispatch("AxisLock(y)")).IsNull();
        await Assert.That(dispatcher.TryDispatch("Workspace(modeling)")).IsNull();
        await Assert.That(cad.Snapshot().Workspace).IsEqualTo("modeling");
    }

    [Test]
    public async Task TryDispatch_MoveSelectedEntity()
    {
        var (doc, dispatcher) = Create();
        dispatcher.TryDispatch("Box(1,1,1)");
        var box = doc.Document.Entities.Last(e => e.Kind == "box");
        doc.SelectedId = box.Id;
        var before = box.Center![0];

        await Assert.That(dispatcher.TryDispatch("Move(1,0,0)")).IsNull();
        await Assert.That(box.Center![0]).IsEqualTo(before + 1f);
    }

    [Test]
    public async Task TryDispatch_InvalidCommand_ReturnsMessage()
    {
        var (_, dispatcher) = Create();
        var err = dispatcher.TryDispatch("NotARealCommand()");
        await Assert.That(err).IsNotNull();
    }

    [Test]
    public async Task EnterTool_FiresToolChanged()
    {
        var (_, dispatcher) = Create();
        var changed = false;
        dispatcher.ToolChanged += () => changed = true;
        await Assert.That(dispatcher.EnterTool(CadToolKind.Wall)).IsNull();
        await Assert.That(dispatcher.ActiveTool).IsEqualTo(CadToolKind.Wall);
        await Assert.That(changed).IsTrue();
    }
}
