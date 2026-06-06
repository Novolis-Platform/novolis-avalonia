using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using global::Avalonia.Media;
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
            var panel = new StackPanel { Spacing = 2, Margin = new Thickness(2, 4) };
            panel.Children.Add(new TextBlock
            {
                Text = node.Label,
                FontWeight = node.Children.Count > 0 ? global::Avalonia.Media.FontWeight.SemiBold : global::Avalonia.Media.FontWeight.Normal,
            });
            return panel;
        }, node => node.Children);
    }

    public void Bind(LiveGraphNode? root) => ItemsSource = root is null ? Array.Empty<LiveGraphNode>() : new[] { root };
}
