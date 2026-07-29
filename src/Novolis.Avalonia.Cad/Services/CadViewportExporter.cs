using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Novolis.Avalonia.Raylib;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Services;

/// <summary>Unified plan (DrawingContext) + Raylib model/preview PNG + phys export.</summary>
public static class CadViewportExporter
{
    public static string ExportsDirectory(string root) => Path.Combine(root, "exports");

    public static string AllocateTourPath(string root, string kind) =>
        Path.Combine(ExportsDirectory(root), $"{kind}.png");

    public static string AllocatePath(string root, string kind) =>
        Path.Combine(ExportsDirectory(root), $"{kind}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.png");

    public static bool TryExportPlanPng(Control draftViewport, string path)
    {
        try
        {
            draftViewport.UpdateLayout();
            var w = System.Math.Max(1, (int)System.Math.Ceiling(draftViewport.Bounds.Width));
            var h = System.Math.Max(1, (int)System.Math.Ceiling(draftViewport.Bounds.Height));
            if (w < 2 || h < 2)
                return false;

            using var bitmap = new RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
            bitmap.Render(draftViewport);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);
            using var stream = File.Create(path);
            bitmap.Save(stream);
            return stream.Length > 32;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string?> ExportModelPngAsync(
        RaylibHostControl host,
        string path,
        CancellationToken cancellationToken = default)
    {
        host.SetHostActive(true);
        host.EnsureHostStarted();

        for (var i = 0; i < 48; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            host.RequestFrame();
            host.InvalidateVisual();
            await Task.Delay(40, cancellationToken).ConfigureAwait(true);
            if (host.HasPresentedFrame && host.TrySaveLastPresentedFramePng(path))
                return path;
        }

        return null;
    }

    public static Task<string?> ExportCurrentPreviewPngAsync(
        RaylibHostControl host,
        string path,
        CancellationToken cancellationToken = default) =>
        ExportModelPngAsync(host, path, cancellationToken);

    public static async Task<IReadOnlyList<string>> ExportViewTourAsync(
        RaylibHostControl host,
        IReadOnlyList<(string Kind, Action SetView)> views,
        string exportRoot,
        CancellationToken cancellationToken = default)
    {
        var saved = new List<string>();
        foreach (var (kind, setView) in views)
        {
            setView();
            var path = AllocateTourPath(exportRoot, kind);
            var ok = await ExportModelPngAsync(host, path, cancellationToken).ConfigureAwait(true);
            if (ok is not null)
                saved.Add(ok);
        }

        return saved;
    }

    public static string ExportPhys(CadDocument document, string path, string? baseDocumentRelative = null)
    {
        var exporter = new CadPhysExporter();
        var phys = exporter.Build(document, baseDocumentRelative ?? Path.GetFileName(path));
        exporter.Write(phys, path);
        return path;
    }
}