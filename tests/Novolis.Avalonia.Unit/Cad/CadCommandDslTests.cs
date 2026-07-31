using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Session;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadCommandDslTests
{
    [Test]
    public async Task Script_Line_With_Points_And_Circle()
    {
        var settings = new CadEditorSettings(Path.Combine(Path.GetTempPath(), "cad-dsl-" + Guid.NewGuid().ToString("N")));
        var document = new CadDocumentSession(settings);
        var bus = new CadCommandBus(document);
        var dispatcher = new CadCommandDispatcher(document, bus, settings);
        var session = new CadSessionService(document, settings, bus, dispatcher);

        var result = session.Execute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.RunCommand,
            Prompt = "Line(Point(0.0,1.0), Point(1.0,1.0)); Circle(Point(2.0,2.0), 0.5);",
        });
        await Assert.That(result.Ok).IsTrue().Because(result.Message);
        await Assert.That(document.Document.Entities.Count(e => e.Kind == "line")).IsEqualTo(1);
        await Assert.That(document.Document.Entities.Count(e => e.Kind == "circle")).IsEqualTo(1);
    }

    [Test]
    public async Task Script_Rect_Extrude_Material()
    {
        var settings = new CadEditorSettings(Path.Combine(Path.GetTempPath(), "cad-dsl2-" + Guid.NewGuid().ToString("N")));
        var document = new CadDocumentSession(settings);
        var bus = new CadCommandBus(document);
        var dispatcher = new CadCommandDispatcher(document, bus, settings);
        _ = new CadSessionService(document, settings, bus, dispatcher);

        var err = dispatcher.TryDispatch(
            "Rect(Point(0,0), Point(4,3)); Extrude(2.4); Material(\"Concrete\");");
        await Assert.That(err).IsNull().Because(err ?? "");
        await Assert.That(document.Document.Entities.Any(e => e.Kind == "box")).IsTrue();
        await Assert.That(document.SelectedEntity?.Material).IsEqualTo("Concrete");
    }
}
