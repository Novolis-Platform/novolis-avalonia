using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Services;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Opens the shaded Render popup and saves PNGs from it.</summary>
public static class SceneRenderActions
{
    private static SceneRenderWindow? _open;

    public static SceneRenderWindow? OpenWindow => _open is { IsVisible: true } ? _open : null;

    public static void ShowRenderWindow(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        var owner = TopLevel.GetTopLevel(host) as Window;
        if (_open is { IsVisible: true })
        {
            _open.Activate();
            _open.SyncFromMainViewport();
            return;
        }

        SceneViewportCamera? mainCam = host is SceneEditorSurface surface ? surface.Viewport.Camera : null;
        var win = new SceneRenderWindow(session, mainCam, notice);
        _open = win;
        win.Closed += (_, _) =>
        {
            if (ReferenceEquals(_open, win))
                _open = null;
        };

        if (owner is not null)
        {
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.Show(owner);
        }
        else
        {
            win.Show();
        }

        notice?.Invoke("Render window open — preview matched to main viewport.");
    }

    /// <summary>Push main-viewport or active-camera framing into the open render preview (if any).</summary>
    public static void SyncOpenPreviewFromMain() => OpenWindow?.SyncFromMainViewport();

    public static void SyncOpenPreviewFromActiveCamera() => OpenWindow?.SyncFromActiveCamera();

    public static void SaveRenderPng(Control host, SceneSessionService session, Action<string>? notice = null) =>
        _ = RunSafe(() => SaveRenderPngAsync(host, session, notice), notice);

    public static async Task SaveRenderPngAsync(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        if (_open is null || !_open.IsVisible)
            ShowRenderWindow(host, session, notice);

        if (_open is null)
        {
            notice?.Invoke("Render window unavailable.");
            return;
        }

        await _open.SavePngAsync().ConfigureAwait(true);
    }

    public static void EnsureStudioLights(SceneSessionService session, Action<string>? notice = null)
    {
        var lights = session.Document.Nodes.OfType<LightNode>().ToList();
        if (lights.Count >= 3)
        {
            notice?.Invoke($"Scene already has {lights.Count} lights.");
            return;
        }

        void Add(string name, string kind, float intensity, float x, float y, float z, float rx, float ry, float rz)
        {
            session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.AddLight,
                LightKind = kind,
                Intensity = intensity,
                Name = name,
            });
            if (session.Document.SelectionId is { } id)
            {
                session.Execute(new AgentCommand
                {
                    ActionId = SceneSessionActionIds.SetTransform,
                    NodeId = id.ToString(),
                    X = x, Y = y, Z = z,
                    Rx = rx, Ry = ry, Rz = rz,
                });
            }
        }

        if (lights.All(l => !l.Name.Contains("Key", StringComparison.OrdinalIgnoreCase)))
            Add("Key", "spot", 3.8f, 22f, 16f, 18f, 40f, -35f, 0f);
        if (lights.All(l => !l.Name.Contains("Fill", StringComparison.OrdinalIgnoreCase)))
            Add("Fill", "omni", 2.0f, 0f, 2f, -18f, 0f, 0f, 0f);
        if (lights.All(l => !l.Name.Contains("Rim", StringComparison.OrdinalIgnoreCase)))
            Add("Rim", "infinite", 0.55f, 0f, 0f, 0f, -55f, 30f, 0f);

        notice?.Invoke("Studio lights ensured (Key / Fill / Rim).");
    }

    private static async Task RunSafe(Func<Task> work, Action<string>? notice)
    {
        try
        {
            await work().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            notice?.Invoke($"Render failed: {ex.Message}");
        }
    }
}
