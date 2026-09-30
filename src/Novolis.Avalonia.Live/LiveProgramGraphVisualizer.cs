using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Live.Visuals;

namespace Novolis.Avalonia.Live;

/// <summary>Tree interpretation of the compiled program graph.</summary>
public sealed class LiveProgramGraphVisualizer : ILiveVisualizer
{
    readonly LiveProgramGraphView _graph = new();
    readonly TextBlock _caption = new()
    {
        Text = "Compiled program structure (from your editor buffer).",
        Foreground = new SolidColorBrush(Color.Parse("#64748B")),
        FontSize = 12,
        Margin = new Thickness(0, 0, 0, 8),
        TextWrapping = TextWrapping.Wrap,
    };

    readonly StackPanel _root;

    public LiveProgramGraphVisualizer()
    {
        _root = new StackPanel { Spacing = 4 };
        _root.Children.Add(_caption);
        _root.Children.Add(_graph);
    }

    public string Title => "Program graph";
    public Control View => _root;

    public void Bind(LiveVisualizerModel model) => _graph.Bind(model.Graph);
}
