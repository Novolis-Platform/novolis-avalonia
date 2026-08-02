using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadViewportExporterTests
{
    [Test]
    public async Task ExportsDirectory_And_AllocatePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-export-" + Guid.NewGuid().ToString("N"));
        var exports = CadViewportExporter.ExportsDirectory(root);
        await Assert.That(exports.EndsWith("exports", StringComparison.OrdinalIgnoreCase)).IsTrue();

        var path = CadViewportExporter.AllocatePath(root, "plan");
        await Assert.That(path.Contains("plan-", StringComparison.Ordinal)).IsTrue();
        await Assert.That(path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).IsTrue();

        var tour = CadViewportExporter.AllocateTourPath(root, "iso");
        await Assert.That(tour.Contains("iso.png", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ExportPhys_WritesJson()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-phys-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var doc = CadDocumentSession.CreateStarter();
            var path = Path.Combine(root, "draft.cadphys.json");
            var written = CadViewportExporter.ExportPhys(doc, path, "draft.cadjson");
            await Assert.That(written).IsEqualTo(path);
            await Assert.That(File.Exists(path)).IsTrue();
            var text = await File.ReadAllTextAsync(path);
            await Assert.That(text.Length).IsGreaterThan(10);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
