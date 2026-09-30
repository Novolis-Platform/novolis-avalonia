using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

public sealed class ModifierStackPanel : UserControl
{
    private readonly SceneSessionService _session;
    private readonly StackPanel _body = new() { Margin = new Thickness(8), Spacing = 4 };

    public ModifierStackPanel(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Content = new DockPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Modifier Stack",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 8, 8, 4),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                new ScrollViewer { Content = _body },
            },
        };
        _session.DocumentChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        _body.Children.Clear();
        var id = _session.Document.SelectionId;
        if (id is null)
        {
            _body.Children.Add(Chrome.Label("Select a mesh."));
            return;
        }

        var mods = _session.Document.Nodes.OfType<ModifierNode>()
            .Where(m => m.InputId == id)
            .ToList();
        if (mods.Count == 0)
        {
            _body.Children.Add(Chrome.Label("No modifiers on selection."));
            return;
        }

        foreach (var mod in mods)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            row.Children.Add(Chrome.Label($"{mod.Name} ({mod.Modifier})"));
            var del = Chrome.Btn("×", () =>
            {
                _session.Document.SelectionId = mod.Id;
                _session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.Delete });
            });
            row.Children.Add(del);
            _body.Children.Add(row);
        }
    }
}
