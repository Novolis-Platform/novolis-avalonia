using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>CAD | Modeling | Preview workspace switcher.</summary>
public sealed class CadWorkspaceBar : StackPanel
{
    private readonly Button _cad;
    private readonly Button _modeling;
    private readonly Button _preview;
    private CadWorkspace _workspace = CadWorkspace.Cad;

    public CadWorkspaceBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        _cad = Make("CAD", CadWorkspace.Cad);
        _modeling = Make("Modeling", CadWorkspace.Modeling);
        _preview = Make("Preview", CadWorkspace.Preview);
        Children.Add(_cad);
        Children.Add(_modeling);
        Children.Add(_preview);
        Refresh();
    }

    public CadWorkspace Workspace
    {
        get => _workspace;
        set
        {
            if (_workspace == value)
                return;
            _workspace = value;
            Refresh();
            WorkspaceChanged?.Invoke(value);
        }
    }

    public event Action<CadWorkspace>? WorkspaceChanged;

    private Button Make(string label, CadWorkspace ws)
    {
        var btn = new Button
        {
            Content = label,
            Padding = new Thickness(10, 4),
            MinWidth = 72,
        };
        btn.Click += (_, _) => Workspace = ws;
        return btn;
    }

    private void Refresh()
    {
        Style(_cad, _workspace == CadWorkspace.Cad);
        Style(_modeling, _workspace == CadWorkspace.Modeling);
        Style(_preview, _workspace == CadWorkspace.Preview);
    }

    private static void Style(Button btn, bool active)
    {
        btn.FontWeight = active ? FontWeight.Bold : FontWeight.Normal;
        btn.Opacity = active ? 1 : 0.75;
    }
}
