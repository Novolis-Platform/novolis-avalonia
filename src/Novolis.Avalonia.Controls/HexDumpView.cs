using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>Read-only hex dump panel with monospace text.</summary>
public sealed class HexDumpView : ScrollViewer
{
    private readonly TextBlock _text;

    /// <summary>Creates an empty hex dump view with monospace font.</summary>
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

    /// <summary>Formats and displays the given byte span.</summary>
    /// <param name="data">Raw bytes to show.</param>
    public void SetBytes(ReadOnlySpan<byte> data) => _text.Text = HexDumpFormatter.Format(data);

    /// <summary>Displays preformatted hex dump text.</summary>
    /// <param name="text">Formatted dump lines.</param>
    public void SetText(string text) => _text.Text = text;

    /// <summary>Clears the display.</summary>
    public void Clear() => _text.Text = string.Empty;
}
