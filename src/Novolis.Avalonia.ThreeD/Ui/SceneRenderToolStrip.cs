using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

/// <summary>Main-window Render group — opens shaded preview popup, save PNG, studio lights.</summary>
public sealed class SceneRenderToolStrip : StackPanel
{
    public SceneRenderToolStrip(SceneSessionService session, Func<Control> host)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(host);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);

        void Notice(string message)
        {
            // Status bar is on the surface; surface hosts pass notice via StatusBar when available.
            if (host() is SceneEditorSurface surface)
                surface.StatusBar.SetNotice(message);
        }

        Children.Add(Chrome.PrimaryBtn("Render…", () =>
            SceneRenderActions.ShowRenderWindow(host(), session, Notice)));
        Children.Add(Chrome.Btn("Save PNG…", () =>
            SceneRenderActions.SaveRenderPng(host(), session, Notice)));
        Children.Add(Chrome.Btn("Studio", () =>
            SceneRenderActions.EnsureStudioLights(session, Notice)));
    }
}
