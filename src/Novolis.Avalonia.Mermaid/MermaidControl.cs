using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Markup.Mermaid;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace Novolis.Avalonia.Mermaid;

/// <summary>
/// Avalonia control that renders Mermaid diagram source (or an <see cref="IMermaidable"/> builder)
/// to SVG via <c>Novolis.Markup.Mermaid.Rendering</c> and displays it in an HTML panel.
/// </summary>
public sealed class MermaidControl : Border
{
    /// <summary>Raw Mermaid source. Used when <see cref="Diagram"/> is null.</summary>
    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<MermaidControl, string?>(nameof(Source));

    /// <summary>Optional fluent Mermaid builder. When set, takes precedence over <see cref="Source"/>.</summary>
    public static readonly StyledProperty<IMermaidable?> DiagramProperty =
        AvaloniaProperty.Register<MermaidControl, IMermaidable?>(nameof(Diagram));

    /// <summary>Color theme for SVG render.</summary>
    public static readonly StyledProperty<MermaidTheme> DiagramThemeProperty =
        AvaloniaProperty.Register<MermaidControl, MermaidTheme>(nameof(DiagramTheme), MermaidTheme.StudioDark);

    /// <summary>Last successfully rendered SVG (null when showing fallback source).</summary>
    public static readonly DirectProperty<MermaidControl, string?> SvgProperty =
        AvaloniaProperty.RegisterDirect<MermaidControl, string?>(nameof(Svg), o => o.Svg);

    private HtmlPanel? _html;
    private ScrollViewer? _scroll;
    private string? _svg;

    /// <summary>Creates an empty Mermaid control.</summary>
    public MermaidControl()
    {
        BorderThickness = new Thickness(0);
        ClipToBounds = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
    }

    /// <summary>Gets or sets raw Mermaid source.</summary>
    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Gets or sets a fluent diagram builder.</summary>
    public IMermaidable? Diagram
    {
        get => GetValue(DiagramProperty);
        set => SetValue(DiagramProperty, value);
    }

    /// <summary>Gets or sets the render theme.</summary>
    public MermaidTheme DiagramTheme
    {
        get => GetValue(DiagramThemeProperty);
        set => SetValue(DiagramThemeProperty, value);
    }

    /// <summary>Gets the last rendered SVG text, when available.</summary>
    public string? Svg
    {
        get => _svg;
        private set => SetAndRaise(SvgProperty, ref _svg, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureHost();
        Refresh();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty ||
            change.Property == DiagramProperty ||
            change.Property == DiagramThemeProperty)
        {
            Refresh();
        }
    }

    /// <summary>Forces a re-render from current <see cref="Diagram"/> / <see cref="Source"/>.</summary>
    public void Refresh()
    {
        var mermaid = ResolveSource();
        if (string.IsNullOrWhiteSpace(mermaid))
        {
            Svg = null;
            if (_html is not null)
                _html.Text = string.Empty;
            return;
        }

        Svg = MermaidSvg.TryRenderSvg(mermaid, DiagramTheme);
        if (_html is null)
            return;

        var html = Svg is not null
            ? MermaidSvg.TryRenderHtmlImage(mermaid, DiagramTheme) ?? MermaidSvg.RenderFallbackPre(mermaid)
            : MermaidSvg.RenderFallbackPre(mermaid);
        _html.Text = WrapDocument(html, DiagramTheme);
    }

    private void EnsureHost()
    {
        if (_html is not null)
            return;

        _html = new HtmlPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
        };

        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _html,
        };

        Child = _scroll;
    }

    private string? ResolveSource()
    {
        if (Diagram is not null)
            return Diagram.GetMermaidString();
        return Source;
    }

    private static string WrapDocument(string body, MermaidTheme theme)
    {
        var bg = theme == MermaidTheme.GitHubLight ? "#ffffff" : "#1e1e1e";
        var fg = theme == MermaidTheme.GitHubLight ? "#24292f" : "#e8e8e8";
        return $$"""
               <html><body style="margin:0;padding:12px;background:{{bg}};color:{{fg}};font-family:Segoe UI,system-ui,sans-serif;">
               <style>
               .mermaid-diagram img { max-width: 100%; height: auto; display: block; }
               pre.mermaid-source { white-space: pre-wrap; font-size: 12px; }
               </style>
               {{body}}
               </body></html>
               """;
    }
}
