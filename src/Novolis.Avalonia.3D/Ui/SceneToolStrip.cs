using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>
/// Legacy file actions strip. Prefer <see cref="SceneChromeShell"/> for full chrome.
/// </summary>
public sealed class SceneToolStrip : StackPanel
{
    private readonly SceneSessionService _session;
    private readonly Action? _onFit;
    private readonly Action<string>? _notice;

    public SceneToolStrip(SceneSessionService session, Action? onFit = null, Action<string>? notice = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _onFit = onFit;
        _notice = notice;
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 6);

        Children.Add(Btn("New", () => Cmd(SceneSessionActionIds.New)));
        Children.Add(Btn("Open…", () => SceneFileActions.Open(this, _session, _notice)));
        Children.Add(Btn("Save", () => SceneFileActions.Save(this, _session, _notice)));
        Children.Add(Btn("Save As…", () => SceneFileActions.SaveAs(this, _session, _notice)));
        Children.Add(Btn("Import…", () => SceneFileActions.ImportMesh(this, _session, _notice)));
        Children.Add(Btn("Fit", () =>
        {
            _onFit?.Invoke();
            Cmd(SceneSessionActionIds.Fit);
        }));
        Children.Add(Btn("Delete", () => Cmd(SceneSessionActionIds.Delete)));
    }

    private void Cmd(string actionId) =>
        _session.Execute(new AgentCommandDto { ActionId = actionId });

    private static Button Btn(string label, Action onClick)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromRgb(32, 48, 62)),
            Foreground = Brushes.WhiteSmoke,
            FontSize = 12,
        };
        b.Click += (_, _) => onClick();
        return b;
    }
}
