using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Novolis.Avalonia.Agent.Protocol.Dto;

namespace Novolis.Avalonia.Agent;

internal static class AgentScreenshot
{
    public static UiScreenshotResponseDto Capture(Window window, string? controlId, int? maxWidth, long requestId)
    {
        Visual? target = window;
        if (!string.IsNullOrWhiteSpace(controlId))
        {
            var match = AgentTreeWalker.FindById(window, controlId);
            if (match is null)
                return new UiScreenshotResponseDto(requestId, false, $"Control not found: {controlId}", null, 0, 0);
            target = match;
        }

        if (target is Control control)
        {
            control.UpdateLayout();
            if (control.Bounds.Width < 1 || control.Bounds.Height < 1)
                return new UiScreenshotResponseDto(requestId, false, "Control has zero size.", null, 0, 0);
        }

        // OpenGL viewports are invisible to RenderTargetBitmap — prefer last GL PNG when present.
        if (TryCaptureGlPng(target, maxWidth, requestId, out var glShot))
            return glShot;

        var pixelSize = target is Control c
            ? new PixelSize(Math.Max(1, (int)Math.Ceiling(c.Bounds.Width)), Math.Max(1, (int)Math.Ceiling(c.Bounds.Height)))
            : new PixelSize(Math.Max(1, (int)window.ClientSize.Width), Math.Max(1, (int)window.ClientSize.Height));

        if (maxWidth is > 0 && pixelSize.Width > maxWidth.Value)
        {
            var scale = maxWidth.Value / (double)pixelSize.Width;
            pixelSize = new PixelSize(maxWidth.Value, Math.Max(1, (int)Math.Round(pixelSize.Height * scale)));
        }

        using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
        bitmap.Render(target);

        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return new UiScreenshotResponseDto(requestId, true, null, stream.ToArray(), pixelSize.Width, pixelSize.Height);
    }

    static bool TryCaptureGlPng(Visual root, int? maxWidth, long requestId, out UiScreenshotResponseDto response)
    {
        response = default!;
        foreach (var visual in root.GetSelfAndVisualDescendants())
        {
            if (visual is not Control control)
                continue;
            var method = control.GetType().GetMethod("TryGetLastFramePng", BindingFlags.Instance | BindingFlags.Public);
            if (method is null || method.ReturnType != typeof(byte[]))
                continue;

            var png = method.Invoke(control, null) as byte[];
            if (png is not { Length: > 0 })
                continue;

            // Width/height unknown without decode; report control bounds as hint.
            var w = Math.Max(1, (int)Math.Ceiling(control.Bounds.Width));
            var h = Math.Max(1, (int)Math.Ceiling(control.Bounds.Height));
            if (maxWidth is > 0 && w > maxWidth.Value)
            {
                var scale = maxWidth.Value / (double)w;
                w = maxWidth.Value;
                h = Math.Max(1, (int)Math.Round(h * scale));
            }

            response = new UiScreenshotResponseDto(requestId, true, null, png, w, h);
            return true;
        }

        return false;
    }
}
