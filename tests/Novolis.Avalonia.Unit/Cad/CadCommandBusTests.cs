using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadCommandBusTests
{
    private static (CadDocumentSession Session, CadCommandBus Bus) Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-bus-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var session = new CadDocumentSession(settings);
        session.NewDocument();
        return (session, new CadCommandBus(session));
    }

    [Test]
    public async Task UndoRedo_RoundTripsAddAndDelete()
    {
        var (session, bus) = Create();
        var entity = new CadEntity { Kind = "line", A = [0f, 0f, 0f], B = [1f, 0f, 0f] };
        bus.Execute(new AddEntityCommand(entity));
        await Assert.That(session.Document.Entities.Any(e => e.Id == entity.Id)).IsTrue();
        await Assert.That(bus.CanUndo).IsTrue();
        await Assert.That(bus.CanRedo).IsFalse();

        bus.Undo();
        await Assert.That(session.Document.Entities.Any(e => e.Id == entity.Id)).IsFalse();
        await Assert.That(bus.CanRedo).IsTrue();

        bus.Redo();
        await Assert.That(session.Document.Entities.Any(e => e.Id == entity.Id)).IsTrue();
    }

    [Test]
    public async Task DeleteEntitiesCommand_RemovesSelection()
    {
        var (session, bus) = Create();
        var a = new CadEntity { Kind = "box", Center = [0f, 0f, 0f], HalfExtents = [0.5f, 0.5f, 0.5f] };
        var b = new CadEntity { Kind = "box", Center = [2f, 0f, 0f], HalfExtents = [0.5f, 0.5f, 0.5f] };
        bus.Execute(new AddEntityCommand(a));
        bus.Execute(new AddEntityCommand(b));
        session.SelectedId = a.Id;

        bus.Execute(new DeleteEntitiesCommand([a.Id]));
        await Assert.That(session.Document.Entities.Any(e => e.Id == a.Id)).IsFalse();
        await Assert.That(session.Document.Entities.Any(e => e.Id == b.Id)).IsTrue();
        await Assert.That(session.SelectedId).IsNull();

        bus.Undo();
        await Assert.That(session.Document.Entities.Any(e => e.Id == a.Id)).IsTrue();
    }

    [Test]
    public async Task MoveEntitiesCommand_TranslatesAndUndoes()
    {
        var (session, bus) = Create();
        var box = new CadEntity { Kind = "box", Center = [0f, 0f, 0f], HalfExtents = [0.5f, 0.5f, 0.5f] };
        bus.Execute(new AddEntityCommand(box));
        bus.Execute(new MoveEntitiesCommand([box.Id], 1f, 2f, 3f));
        await Assert.That(box.Center![0]).IsEqualTo(1f);
        await Assert.That(box.Center[1]).IsEqualTo(2f);
        await Assert.That(box.Center[2]).IsEqualTo(3f);

        bus.Undo();
        await Assert.That(box.Center![0]).IsEqualTo(0f);
        await Assert.That(box.Center[1]).IsEqualTo(0f);
        await Assert.That(box.Center[2]).IsEqualTo(0f);
    }

    [Test]
    public async Task MutateEntityGeometryCommand_CapturesAndRestores()
    {
        var (session, bus) = Create();
        var circle = new CadEntity
        {
            Kind = "circle",
            Center = [0f, 0f, 0f],
            Radius = 1f,
        };
        bus.Execute(new AddEntityCommand(circle));
        var before = EntityGeometrySnapshot.Capture(circle);
        circle.Radius = 2.5f;
        var after = EntityGeometrySnapshot.Capture(circle);

        bus.Execute(new MutateEntityGeometryCommand(circle.Id, before, after));
        await Assert.That(circle.Radius).IsEqualTo(2.5f);

        bus.Undo();
        await Assert.That(circle.Radius).IsEqualTo(1f);
    }

    [Test]
    public async Task MutateEntityFieldsCommand_AppliesMaterial()
    {
        var (session, bus) = Create();
        var box = new CadEntity { Kind = "box", Center = [0f, 0f, 0f], HalfExtents = [0.5f, 0.5f, 0.5f] };
        bus.Execute(new AddEntityCommand(box));

        bus.Execute(new MutateEntityFieldsCommand(
            box.Id,
            "Set material",
            e => e.Material = "Steel",
            e => e.Material = null));
        await Assert.That(box.Material).IsEqualTo("Steel");

        bus.Undo();
        await Assert.That(box.Material).IsNull();
    }

    [Test]
    public async Task EntityGeometrySnapshot_RoundTripsSplineFields()
    {
        var entity = new CadEntity
        {
            Kind = "spline",
            ControlPoints = [[0f, 0f, 0f], [1f, 0f, 0f]],
            Knots = [0f, 0f, 1f, 1f],
            Weights = [1f, 1f],
            FitPoints = [[0f, 0f, 0f], [1f, 0f, 1f]],
            Closed = true,
        };
        var snap = EntityGeometrySnapshot.Capture(entity);
        entity.ControlPoints = null;
        entity.Knots = null;
        snap.ApplyTo(entity);
        await Assert.That(entity.ControlPoints).IsNotNull();
        await Assert.That(entity.ControlPoints!.Count).IsEqualTo(2);
        await Assert.That(entity.Closed).IsTrue();
    }
}
