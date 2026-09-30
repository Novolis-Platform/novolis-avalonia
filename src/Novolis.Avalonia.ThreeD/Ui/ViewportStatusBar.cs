using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

public sealed class ViewportStatusBar : UserControl
{
    private readonly SceneSessionService _session;
    private readonly TextBlock _text = new()
    {
        Margin = new Thickness(10, 4),
        FontSize = 12,
        Opacity = 0.9,
        Foreground = Brushes.WhiteSmoke,
    };
    private string? _notice;

    public ViewportStatusBar(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Content = _text;
        _session.DocumentChanged += () => Refresh(_session);
        Refresh(_session);
    }

    public void SetNotice(string? notice)
    {
        _notice = notice;
        Refresh(_session);
    }

    public void Refresh(SceneSessionService session)
    {
        var edit = session.Document.Edit;
        var pathHint = string.IsNullOrWhiteSpace(session.DocumentPath)
            ? "unsaved"
            : Path.GetFileName(session.DocumentPath);
        var baseLine =
            $"{session.Document.Name} · {pathHint} · {edit.Mode} · {edit.DisplayMode} · components={edit.SelectionCount} · nodes={session.Document.Nodes.Count}";
        _text.Text = string.IsNullOrWhiteSpace(_notice) ? baseLine : $"{baseLine} · {_notice}";
    }
}
