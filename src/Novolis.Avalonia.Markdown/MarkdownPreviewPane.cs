using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace Novolis.Avalonia.Markdown;

/// <summary>Live HTML preview of Markdown source with theme support, side margins, and Ctrl+scroll zoom.</summary>
public sealed class MarkdownPreviewPane : Border
{
    public const double DefaultSideMarginFraction = 0.10;

    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, string?>(nameof(Markdown), string.Empty);

    public static readonly StyledProperty<string?> DocumentBodyHtmlProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, string?>(nameof(DocumentBodyHtml));

    public static readonly StyledProperty<double> ZoomScaleProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, double>(nameof(ZoomScale), MarkdownZoom.Default);

    public static readonly StyledProperty<MarkdownPreviewTheme> PreviewThemeProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, MarkdownPreviewTheme>(nameof(PreviewTheme), MarkdownPreviewTheme.StudioDark);

    public static readonly StyledProperty<double> SideMarginFractionProperty =
        AvaloniaProperty.Register<MarkdownPreviewPane, double>(nameof(SideMarginFraction), DefaultSideMarginFraction);

    private readonly Panel _extentHost = new();
    private readonly HtmlPanel _html;
    private readonly ScrollViewer _scroll;
    private readonly ScaleTransform _scale;
    private string? _lastBodyHtml;
    private double _lastExtentHeight;

    /// <summary>Creates a styled Markdown preview pane.</summary>
    public MarkdownPreviewPane()
    {
        BorderThickness = new Thickness(0);
        ClipToBounds = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _scale = new ScaleTransform(MarkdownZoom.Default, MarkdownZoom.Default);
        _html = new HtmlPanel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = _scale,
            RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
        };

        _extentHost.Children.Add(_html);

        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = _extentHost,
        };

        Child = _scroll;

        _scroll.SizeChanged += (_, _) => ApplyLayout();
        _html.LayoutUpdated += (_, _) => ApplyLayout();
        CtrlScrollZoom.Attach(_scroll, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_html, () => ZoomScale, value => ZoomScale = value);

        ApplyThemeChrome();
        RefreshHtml();
    }

    /// <summary>Raised when <see cref="ZoomScale"/> changes.</summary>
    public event EventHandler<double>? ZoomScaleChanged;

    /// <summary>Gets or sets the Markdown source to preview when <see cref="DocumentBodyHtml"/> is not set.</summary>
    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <summary>Gets or sets pre-rendered body HTML. When set, takes precedence over <see cref="Markdown"/>.</summary>
    public string? DocumentBodyHtml
    {
        get => GetValue(DocumentBodyHtmlProperty);
        set => SetValue(DocumentBodyHtmlProperty, value);
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

    /// <summary>Fraction of viewport width used as side margin on each side (default 10%).</summary>
    public double SideMarginFraction
    {
        get => GetValue(SideMarginFractionProperty);
        set => SetValue(SideMarginFractionProperty, value);
    }

    /// <summary>Refreshes the preview from the current content.</summary>
    public void Refresh() => RefreshHtml();

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MarkdownProperty || change.Property == DocumentBodyHtmlProperty)
        {
            _lastBodyHtml = ResolveBodyHtml();
            _lastExtentHeight = 0;
            RefreshHtml();
        }
        else if (change.Property == PreviewThemeProperty)
        {
            ApplyThemeChrome();
            _lastExtentHeight = 0;
            RefreshHtml();
        }
        else if (change.Property == ZoomScaleProperty)
        {
            _lastExtentHeight = 0;
            ApplyLayout();
            ZoomScaleChanged?.Invoke(this, ZoomScale);
        }
        else if (change.Property == SideMarginFractionProperty)
        {
            _lastExtentHeight = 0;
            ApplyLayout();
        }
    }

    private void ApplyThemeChrome()
    {
        if (PreviewTheme == MarkdownPreviewTheme.GitHubLight)
        {
            Background = new SolidColorBrush(Color.Parse("#ffffff"));
            _html.Background = new SolidColorBrush(Color.Parse("#ffffff"));
        }
        else
        {
            Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
            _html.Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
        }
    }

    private void RefreshHtml()
    {
        try
        {
            var body = _lastBodyHtml ?? ResolveBodyHtml();
            _html.Text = MarkdownPreviewHtml.WrapDocument(body, PreviewTheme);
            ApplyThemeChrome();
            ApplyLayout();
        }
        catch
        {
            // HtmlRenderer can throw / native-fail on odd HTML (e.g. Review mastheads).
            // Keep the pane alive with a minimal fallback instead of taking down the host.
            try
            {
                var escaped = System.Net.WebUtility.HtmlEncode(Markdown ?? string.Empty)
                    .Replace("\n", "<br/>");
                _html.Text = MarkdownPreviewHtml.WrapDocument($"<p>{escaped}</p>", PreviewTheme);
                ApplyThemeChrome();
                ApplyLayout();
            }
            catch
            {
                // Last resort: leave previous content.
            }
        }
    }

    private string ResolveBodyHtml()
    {
        var explicitBody = DocumentBodyHtml;
        if (explicitBody is not null)
            return explicitBody;

        try
        {
            return MarkdownPreviewHtml.ToBodyHtml(Markdown, PreviewTheme);
        }
        catch
        {
            var escaped = System.Net.WebUtility.HtmlEncode(Markdown ?? string.Empty)
                .Replace("\n", "<br/>");
            return $"<p>{escaped}</p>";
        }
    }

    private void ApplyLayout()
    {
        var zoom = Math.Clamp(ZoomScale, MarkdownZoom.Minimum, MarkdownZoom.Maximum);
        if (Math.Abs(zoom - ZoomScale) > 0.0001)
            SetCurrentValue(ZoomScaleProperty, zoom);

        _scale.ScaleX = zoom;
        _scale.ScaleY = zoom;

        var viewportWidth = _scroll.Viewport.Width;
        if (viewportWidth <= 0)
            return;

        var sideInset = viewportWidth * SideMarginFraction;
        var contentWidth = Math.Max(1, viewportWidth - (2 * sideInset));
        var layoutWidth = contentWidth / zoom;

        _html.Margin = new Thickness(sideInset, 0, sideInset, 0);
        _html.Width = layoutWidth;
        _html.MaxWidth = layoutWidth;

        var contentHeight = MeasureHtmlHeight(layoutWidth);
        if (contentHeight <= 0)
            return;

        var extentHeight = contentHeight * zoom;
        _extentHost.Width = viewportWidth;

        if (Math.Abs(extentHeight - _lastExtentHeight) > 0.5)
        {
            _extentHost.MinHeight = extentHeight;
            _lastExtentHeight = extentHeight;
        }
    }

    private double MeasureHtmlHeight(double layoutWidth)
    {
        if (_html.Bounds.Height > 0)
            return _html.Bounds.Height;

        if (_html.DesiredSize.Height > 0 && _html.DesiredSize.Width > 0)
            return _html.DesiredSize.Height;

        _html.Measure(new Size(layoutWidth, double.PositiveInfinity));
        return _html.DesiredSize.Height;
    }
}
