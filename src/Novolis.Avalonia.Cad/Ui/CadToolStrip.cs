using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>Workspace-specific modeling / CAD / preview tool buttons.</summary>
public sealed class CadToolStrip : StackPanel
{
    private CadWorkspace _workspace = CadWorkspace.Cad;

    public CadToolStrip()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Rebuild();
    }

    public CadWorkspace Workspace
    {
        get => _workspace;
        set
        {
            if (_workspace == value)
                return;
            _workspace = value;
            Rebuild();
        }
    }

    public event Action<string>? ToolRequested;

    private void Rebuild()
    {
        Children.Clear();
        foreach (var (id, label) in ToolsFor(_workspace))
        {
            var capture = id;
            var btn = new Button
            {
                Content = label,
                Padding = new Thickness(8, 3),
            };
            btn.Click += (_, _) => ToolRequested?.Invoke(capture);
            Children.Add(btn);
        }
    }

    private static (string Id, string Label)[] ToolsFor(CadWorkspace ws) =>
        ws switch
        {
            CadWorkspace.Modeling =>
            [
                ("meshFromSolid", "Mesh From Solid"),
                ("weld", "Weld"),
                ("optimize", "Optimize"),
                ("bridge", "Bridge"),
            ],
            CadWorkspace.Preview =>
            [
                ("addMaterial", "Material"),
                ("addLight", "Light"),
                ("addCamera", "Camera"),
            ],
            _ =>
            [
                ("boolean", "Boolean"),
                ("symmetry", "Symmetry"),
                ("clone", "Cloner"),
                ("instance", "Instance"),
                ("connect", "Connect"),
                ("split", "Split"),
                ("group", "Group"),
            ],
        };
}
