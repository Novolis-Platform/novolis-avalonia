using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace Novolis.Avalonia.Markdown;

/// <summary>Live HTML preview of Markdown source with theme support and Ctrl+scroll zoom.</summary>
public sealed class MarkdownPreviewPane : Border
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, string?>(nameof(Markdown), string.Empty);

    public static readonly StyledProperty<double> ZoomScaleProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, double>(nameof(ZoomScale), MarkdownZoom.Default);

    public static readonly StyledProperty<MarkdownPreviewTheme> PreviewThemeProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, MarkdownPreviewTheme>(nameof(PreviewTheme), MarkdownPreviewTheme.StudioDark);

    private readonly HtmlPanel _html;
    private readonly ScrollViewer _scroll;
    private readonly ScaleTransform _scale;

    /// <summary>Creates a styled Markdown preview pane.</summary>
    public MarkdownPreviewPane()
    {
        Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
        BorderBrush = new SolidColorBrush(Color.Parse("#2d2d30"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(4);
        ClipToBounds = true;

        _html = new HtmlPanel
        {
            Margin = new Thickness(0),
        };

        _scale = new ScaleTransform(MarkdownZoom.Default, MarkdownZoom.Default);
        _html.RenderTransform = _scale;

        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _html,
        };

        Child = _scroll;

        CtrlScrollZoom.Attach(this, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_scroll, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_html, () => ZoomScale, value => ZoomScale = value);

        RefreshHtml();
    }

    /// <summary>Gets or sets the Markdown source to preview.</summary>
    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <summary>Gets or sets preview zoom (1.0 = 100%). Adjust with Ctrl+mouse wheel.</summary>
    public double ZoomScale
    {
        get => GetValue(ZoomScaleProperty);
        set => SetValue(ZoomScaleProperty, value);
    }

    /// <summary>Gets or sets the HTML theme applied to the preview.</summary>
    public MarkdownPreviewTheme PreviewTheme
    {
        get => GetValue(PreviewThemeProperty);
        set => SetValue(PreviewThemeProperty, value);
    }

    /// <summary>Refreshes the preview from the current <see cref="Markdown"/> value.</summary>
    public void Refresh() => RefreshHtml();

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MarkdownProperty || change.Property == PreviewThemeProperty)
            RefreshHtml();
        else if (change.Property == ZoomScaleProperty)
            ApplyZoom();
    }

    private void RefreshHtml()
    {
        var html = MarkdownPreviewHtml.FromMarkdown(Markdown, PreviewTheme);
        _html.Text = html;
    }

    private void ApplyZoom()
    {
        _scale.ScaleX = ZoomScale;
        _scale.ScaleY = ZoomScale;
    }
}
