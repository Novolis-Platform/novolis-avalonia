using Novolis.Avalonia.Controls.Sketch;

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
    public async Task Json_RoundTrips_Fill_Style_And_Closed()
    {
        var doc = new SketchDocument();
        doc.AddStroke(new StrokeShape
        {
            Id = "poly",
            StrokeColor = "#123456",
            StrokeWidth = 0.5,
            FillColor = "#abcdef",
            StrokeStyle = SketchStrokeStyle.Stipple,
            Closed = true,
            Points =
            [
                new SketchPoint(0, 0),
                new SketchPoint(10, 0),
                new SketchPoint(10, 10),
                new SketchPoint(0, 0)
            ]
        });

        var loaded = SketchJson.Deserialize(SketchJson.Serialize(doc));
        var s = loaded.Elements[0];
        await Assert.That(s.StrokeWidth).IsEqualTo(0.5);
        await Assert.That(s.FillColor).IsEqualTo("#abcdef");
        await Assert.That(s.StrokeStyle).IsEqualTo(SketchStrokeStyle.Stipple);
        await Assert.That(s.Closed).IsTrue();
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

    [Test]
    public async Task Fuse_And_Ungroup_Share_GroupId()
    {
        var doc = new SketchDocument();
        doc.AddStroke(new StrokeShape { Id = "a", Points = [new SketchPoint(0, 0), new SketchPoint(1, 0)] });
        doc.AddStroke(new StrokeShape { Id = "b", Points = [new SketchPoint(2, 0), new SketchPoint(3, 0)] });
        doc.SetSelection(["a", "b"]);
        await Assert.That(doc.FuseSelection()).IsTrue();
        var ga = doc.Find("a")!.GroupId;
        var gb = doc.Find("b")!.GroupId;
        await Assert.That(ga).IsNotNull();
        await Assert.That(ga).IsEqualTo(gb);

        doc.Select("a");
        await Assert.That(doc.Selection.Count).IsEqualTo(2);
        await Assert.That(doc.UngroupSelection()).IsTrue();
        await Assert.That(doc.Find("a")!.GroupId).IsNull();
        await Assert.That(doc.Find("b")!.GroupId).IsNull();
    }

    [Test]
    public async Task Json_RoundTrips_V2_Fields()
    {
        var doc = new SketchDocument { Version = 2 };
        doc.AddStroke(new StrokeShape
        {
            Id = "t1",
            Kind = SketchElementKind.Text,
            Text = "Hello",
            FontSize = 22,
            RotationDegrees = 15,
            GroupId = "g1",
            StrokeColor = "#112233",
            Points = [new SketchPoint(5, 6), new SketchPoint(40, 30)]
        });
        doc.AddStroke(new StrokeShape
        {
            Id = "img",
            Kind = SketchElementKind.Image,
            ImagePngBase64 = "AQID",
            Points =
            [
                new SketchPoint(0, 0),
                new SketchPoint(10, 0),
                new SketchPoint(10, 10),
                new SketchPoint(0, 10),
                new SketchPoint(0, 0)
            ]
        });

        var loaded = SketchJson.Deserialize(SketchJson.Serialize(doc));
        await Assert.That(loaded.Version).IsGreaterThanOrEqualTo(2);
        var text = loaded.Elements.First(e => e.Id == "t1");
        await Assert.That(text.Kind).IsEqualTo(SketchElementKind.Text);
        await Assert.That(text.Text).IsEqualTo("Hello");
        await Assert.That(text.FontSize).IsEqualTo(22.0);
        await Assert.That(text.RotationDegrees).IsEqualTo(15.0);
        await Assert.That(text.GroupId).IsEqualTo("g1");
        var img = loaded.Elements.First(e => e.Id == "img");
        await Assert.That(img.Kind).IsEqualTo(SketchElementKind.Image);
        await Assert.That(img.ImagePngBase64).IsEqualTo("AQID");
    }

    [Test]
    public async Task ApplyFill_Sets_Fill_And_Closes()
    {
        var doc = new SketchDocument();
        doc.AddStroke(new StrokeShape
        {
            Id = "poly",
            Points =
            [
                new SketchPoint(0, 0),
                new SketchPoint(10, 0),
                new SketchPoint(10, 10),
                new SketchPoint(0, 10)
            ]
        });
        await Assert.That(doc.ApplyFill("poly", "#80ff0000")).IsTrue();
        var s = doc.Find("poly")!;
        await Assert.That(s.FillColor).IsEqualTo("#80ff0000");
        await Assert.That(s.Closed).IsTrue();
    }

    [Test]
    public async Task Json_RoundTrips_Layers()
    {
        var doc = new SketchDocument { Version = 3 };
        var extra = doc.AddLayer("Overlay");
        doc.AddStroke(new StrokeShape
        {
            Id = "a",
            LayerId = extra.Id,
            Points = [new SketchPoint(1, 2), new SketchPoint(3, 4)]
        });
        doc.SetLayerVisible(extra.Id, false);

        var loaded = SketchJson.Deserialize(SketchJson.Serialize(doc));
        await Assert.That(loaded.Version).IsGreaterThanOrEqualTo(3);
        await Assert.That(loaded.Layers.Count).IsEqualTo(2);
        await Assert.That(loaded.Find("a")!.LayerId).IsEqualTo(extra.Id);
        await Assert.That(loaded.FindLayer(extra.Id)!.Visible).IsFalse();
    }

    [Test]
    public async Task Json_Loads_Legacy_V1_Without_Kind()
    {
        const string json = """
            {
              "version": 1,
              "grid": { "size": 20, "visible": true, "snapEnabled": false },
              "elements": [
                {
                  "id": "legacy",
                  "strokeColor": "#000000",
                  "strokeWidth": 2,
                  "points": [ { "x": 1, "y": 2 }, { "x": 3, "y": 4 } ]
                }
              ]
            }
            """;
        var loaded = SketchJson.Deserialize(json);
        await Assert.That(loaded.Elements[0].Kind).IsEqualTo(SketchElementKind.Stroke);
        await Assert.That(loaded.Elements[0].RotationDegrees).IsEqualTo(0.0);
        await Assert.That(loaded.Layers.Count).IsGreaterThanOrEqualTo(1);
    }
}
