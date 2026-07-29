using Novolis.Avalonia.Controls;

namespace Novolis.Avalonia.Unit.Controls;

public sealed class SketchDocumentTests
{
    [Test]
    public async Task GridifySelection_Quantizes_Selected_Stroke()
    {
        var doc = new SketchDocument();
        doc.Grid.Size = 10;
        var stroke = new StrokeShape
        {
            Id = "a",
            Points = [new SketchPoint(3, 3), new SketchPoint(17, 3), new SketchPoint(17, 18)]
        };
        doc.AddStroke(stroke);
        doc.Select("a");
        doc.GridifySelection();

        await Assert.That(doc.Find("a")!.Points).IsEquivalentTo(new[]
        {
            new SketchPoint(0, 0),
            new SketchPoint(20, 0),
            new SketchPoint(20, 20)
        });
    }

    [Test]
    public async Task Undo_Restores_Pre_Gridify_Geometry()
    {
        var doc = new SketchDocument();
        doc.Grid.Size = 10;
        doc.AddStroke(new StrokeShape
        {
            Id = "a",
            Points = [new SketchPoint(3, 3), new SketchPoint(12, 3)]
        });
        doc.Select("a");
        doc.GridifySelection();
        await Assert.That(doc.Undo()).IsTrue();
        await Assert.That(doc.Find("a")!.Points).IsEquivalentTo(new[]
        {
            new SketchPoint(3, 3),
            new SketchPoint(12, 3)
        });
    }

    [Test]
    public async Task Json_RoundTrips_Elements_And_Grid()
    {
        var doc = new SketchDocument { Version = 1 };
        doc.Grid.Size = 25;
        doc.Grid.SnapEnabled = true;
        doc.AddStroke(new StrokeShape
        {
            Id = "s1",
            StrokeColor = "#ff0000",
            StrokeWidth = 3,
            Points = [new SketchPoint(1, 2), new SketchPoint(3, 4)]
        });

        var json = SketchJson.Serialize(doc);
        var loaded = SketchJson.Deserialize(json);

        await Assert.That(loaded.Grid.Size).IsEqualTo(25.0);
        await Assert.That(loaded.Grid.SnapEnabled).IsTrue();
        await Assert.That(loaded.Elements.Count).IsEqualTo(1);
        await Assert.That(loaded.Elements[0].Id).IsEqualTo("s1");
        await Assert.That(loaded.Elements[0].StrokeColor).IsEqualTo("#ff0000");
        await Assert.That(loaded.Elements[0].Points).IsEquivalentTo(new[]
        {
            new SketchPoint(1, 2),
            new SketchPoint(3, 4)
        });
    }

    [Test]
    public async Task Clear_Is_Undoable()
    {
        var doc = new SketchDocument();
        doc.AddStroke(new StrokeShape { Id = "a", Points = [new SketchPoint(0, 0)] });
        doc.Clear();
        await Assert.That(doc.Elements.Count).IsEqualTo(0);
        await Assert.That(doc.Undo()).IsTrue();
        await Assert.That(doc.Elements.Count).IsEqualTo(1);
    }
}
