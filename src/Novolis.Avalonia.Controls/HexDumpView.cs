using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>Read-only hex dump panel with monospace text.</summary>
public sealed class HexDumpView : ScrollViewer
{
    private readonly TextBlock _text;

    public HexDumpView()
    {
        _text = new TextBlock
        {
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            TextWrapping = TextWrapping.NoWrap,
        };
        Content = _text;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    public void SetBytes(ReadOnlySpan<byte> data) => _text.Text = HexDumpFormatter.Format(data);

    public void SetText(string text) => _text.Text = text;

    public void Clear() => _text.Text = string.Empty;
}
