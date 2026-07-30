using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Novolis.Avalonia._3D.Ui;

namespace Novolis.Avalonia._3D.Services;

/// <summary>PNG capture helpers for scene viewports (GL readback or Avalonia RenderTargetBitmap).</summary>
public static class SceneViewportExporter
{
    public static string ExportsDirectory(string root) => Path.Combine(root, "exports");

    public static string DumpsDirectory(string root) => Path.Combine(root, "dumps");

    public static string AllocatePath(string directory, string kind, string extension = "png") =>
        Path.Combine(directory, $"{kind}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.{extension}");

    public static bool TryExportControlPng(Control control, string path)
    {
        try
        {
            control.UpdateLayout();
            var w = System.Math.Max(1, (int)System.Math.Ceiling(control.Bounds.Width));
            var h = System.Math.Max(1, (int)System.Math.Ceiling(control.Bounds.Height));
            if (w < 2 || h < 2)
                return false;

            using var bitmap = new RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
            bitmap.Render(control);
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

    public static async Task<bool> ExportViewportPngAsync(
        SceneViewportControl viewport,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (await viewport.CapturePngAsync(path).ConfigureAwait(true))
            return true;

        // Fallback: Avalonia compose (may be blank for GL until a frame has been presented).
        for (var i = 0; i < 8; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            viewport.RequestPresent();
            await Task.Delay(40, cancellationToken).ConfigureAwait(true);
            if (TryExportControlPng(viewport, path))
                return true;
        }

        return false;
    }
}
