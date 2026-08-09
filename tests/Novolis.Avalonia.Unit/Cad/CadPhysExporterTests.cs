using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Cad.Primitives;
using Novolis.Cad.Evaluation;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadPhysExporterTests
{
    [Test]
    public async Task Build_EmitsCollidersForSolids()
    {
        var doc = new CadDocument { Name = "PhysTest" };
        doc.Entities.Add(new CadEntity
        {
            Kind = "box",
            Name = "Crate",
            Center = [0f, 0.5f, 0f],
            HalfExtents = [0.5f, 0.5f, 0.5f],
            Material = "Steel",
        });
        doc.Entities.Add(new CadEntity
        {
            Kind = "sphere",
            Name = "Ball",
            Center = [2f, 1f, 0f],
            Radius = 0.5f,
        });

        var exporter = new CadPhysExporter();
        var phys = exporter.Build(doc, "test.cadjson");
        await Assert.That(phys.Meshes.Count).IsEqualTo(2);
        await Assert.That(phys.Colliders.Count).IsEqualTo(2);
        await Assert.That(phys.BaseDocument).IsEqualTo("test.cadjson");
    }

    [Test]
    public async Task Write_RoundTripsJson()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-phys2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var doc = CadDocumentSession.CreateStarter();
            var path = Path.Combine(root, "out.cadphys.json");
            var exporter = new CadPhysExporter();
            var phys = exporter.Build(doc);
            exporter.Write(phys, path);
            await Assert.That(File.Exists(path)).IsTrue();
            var text = await File.ReadAllTextAsync(path);
            await Assert.That(text.Contains("colliders", StringComparison.Ordinal)).IsTrue();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
