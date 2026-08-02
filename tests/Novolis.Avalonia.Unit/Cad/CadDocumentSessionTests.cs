using Novolis.Avalonia.Cad.Core;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Unit.Cad;

public sealed class CadDocumentSessionTests
{
    [Test]
    public async Task SaveTo_And_OpenFromPath_RoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-doc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.NewDocument();
            var path = Path.Combine(root, "test.cadjson");
            session.SaveTo(path);
            await Assert.That(File.Exists(path)).IsTrue();

            session.OpenFromPath(path);
            await Assert.That(session.IsDirty).IsFalse();
            await Assert.That(session.Document.Format).IsEqualTo("novolis.cad");
            await Assert.That(session.Document.Entities.Count).IsGreaterThan(0);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task SetSelection_Additive_KeepsPriorSelection()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-sel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var session = new CadDocumentSession(settings);
        session.NewDocument();
        var a = session.Document.Entities[0].Id;
        var b = session.Document.Entities[1].Id;

        session.SetSelection(a);
        session.SetSelection(b, additive: true);
        await Assert.That(session.SelectedId).IsEqualTo(b);
        await Assert.That(session.SelectedIds).Contains(a);
        await Assert.That(session.SelectedIds).Contains(b);
    }

    [Test]
    public async Task OpenOrCreateDefault_CreatesStarterWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-open-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settings = new CadEditorSettings(root);
            var session = new CadDocumentSession(settings);
            session.OpenOrCreateDefault();
            await Assert.That(File.Exists(settings.DocumentPath)).IsTrue();
            await Assert.That(session.Document.Entities.Count).IsGreaterThan(0);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task MarkDirty_RaisesChanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-dirty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var session = new CadDocumentSession(settings);
        session.NewDocument();
        var fired = false;
        session.Changed += () => fired = true;
        session.MarkDirty();
        await Assert.That(fired).IsTrue();
        await Assert.That(session.IsDirty).IsTrue();
    }

    [Test]
    public async Task SelectedEntity_ReturnsEntityOrNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-cad-entity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var settings = new CadEditorSettings(root);
        var session = new CadDocumentSession(settings);
        session.NewDocument();
        await Assert.That(session.SelectedEntity).IsNull();
        var id = session.Document.Entities[0].Id;
        session.SelectedId = id;
        await Assert.That(session.SelectedEntity).IsNotNull();
        await Assert.That(session.SelectedEntity!.Id).IsEqualTo(id);
    }
}
