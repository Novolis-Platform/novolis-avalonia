using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Novolis.Audio.Live.Visuals;

namespace Novolis.Avalonia.Live;

/// <summary>Shows how the editor source maps to interpreted structure.</summary>
public sealed class LiveCodeInterpretationVisualizer : ILiveVisualizer
{
    readonly TextBlock _body = new()
    {
        FontFamily = new FontFamily("Cascadia Mono,Consolas,monospace"),
        FontSize = 13,
        Foreground = new SolidColorBrush(Color.Parse("#E2E8F0")),
        TextWrapping = TextWrapping.Wrap,
    };

    public string Title => "Code interpretation";
    public Control View => new ScrollViewer { Content = _body };

    public void Bind(LiveVisualizerModel model)
    {
        if (model.Graph is null)
        {
            _body.Text = "Compile to interpret the buffer into tracks / patterns.";
            return;
        }

        var lines = new List<string>
        {
            $"Preset: {model.ActivePreset ?? "Live buffer"}",
            $"Transport: beat {model.Beat:0.###} · bar {model.Bar} · phrase {model.Phrase} @ {model.Bpm:0} BPM",
            "",
            "Interpreted graph:",
        };
        AppendNode(lines, model.Graph, indent: 0);
        if (!string.IsNullOrWhiteSpace(model.SourceExcerpt))
        {
            lines.Add("");
            lines.Add("Source excerpt:");
            lines.Add(model.SourceExcerpt.Trim());
        }

        _body.Text = string.Join(Environment.NewLine, lines);
    }

    static void AppendNode(List<string> lines, LiveGraphNode node, int indent)
    {
        lines.Add($"{new string(' ', indent * 2)}- {node.Label}");
        foreach (var child in node.Children)
            AppendNode(lines, child, indent + 1);
    }
}
