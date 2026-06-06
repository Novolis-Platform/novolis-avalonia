using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Novolis.Audio.Live.Visuals;

namespace Novolis.Avalonia.Live;

public sealed class LiveProgramGraphView : TreeView
{
    public LiveProgramGraphView()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        ItemTemplate = new FuncTreeDataTemplate<LiveGraphNode>((node, _) =>
        {
            var panel = new StackPanel { Spacing = 2 };
            panel.Children.Add(new TextBlock { Text = node.Label });
            return panel;
        }, node => node.Children);
    }

    public void Bind(LiveGraphNode? root) => ItemsSource = root is null ? Array.Empty<LiveGraphNode>() : new[] { root };
}
