using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaloniaEdit;

namespace Novolis.Avalonia.Markdown;

/// <summary>
/// Markdown source editor built on AvaloniaEdit with line numbers, word wrap,
/// current-line highlight, Ctrl+scroll zoom, and OS light/dark chrome.
/// </summary>
public sealed class MarkdownSourceEditor : Border
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarkdownSourceEditor, string?>(nameof(Text), string.Empty);

    public static readonly StyledProperty<double> ZoomScaleProperty =
        AvaloniaProperty.Register<MarkdownSourceEditor, double>(nameof(ZoomScale), MarkdownZoom.Default);

    public static readonly StyledProperty<bool> WordWrapProperty =
        AvaloniaProperty.Register<MarkdownSourceEditor, bool>(nameof(WordWrap), true);

    public static readonly StyledProperty<double> BaseFontSizeProperty =
        AvaloniaProperty.Register<MarkdownSourceEditor, double>(nameof(BaseFontSize), 14.0);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<MarkdownSourceEditor, string?>(nameof(PlaceholderText), "Write Markdown…");

    public static readonly StyledProperty<MarkdownSourceHighlightingProfile> HighlightingProfileProperty =
        AvaloniaProperty.Register<MarkdownSourceEditor, MarkdownSourceHighlightingProfile>(
            nameof(HighlightingProfile),
            MarkdownSourceHighlightingProfile.Markdown);

    readonly TextEditor _editor;
    bool _themeHooked;

    /// <summary>Creates a styled Markdown source editor.</summary>
    public MarkdownSourceEditor()
    {
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(4);
        ClipToBounds = true;

        _editor = new TextEditor
        {
            FontFamily = EditorFontFamily,
            ShowLineNumbers = true,
            WordWrap = true,
            LineNumbersMargin = new Thickness(8, 10, 6, 10),
            Padding = new Thickness(0, 10, 12, 10),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
        };

        _editor.Options.HighlightCurrentLine = true;
        _editor.Options.AcceptsTab = true;
        _editor.TextArea.SelectionForeground = Brushes.White;

        Child = _editor;
        _editor.TextChanged += OnEditorTextChanged;
        _editor.TextArea.Caret.PositionChanged += (_, _) =>
        {
            // Keep caret visible while typing near the soft keyboard.
            if (VisualRoot is not null)
                EnsureCaretVisible();
        };

        CtrlScrollZoom.Attach(this, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_editor, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_editor.TextArea, () => ZoomScale, value => ZoomScale = value);

        _editor.Watermark = PlaceholderText ?? string.Empty;
        UpdateTypography();
        ApplySyntaxHighlighting();
        ApplyThemeChrome(ResolveLight());

        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    /// <summary>Shared monospace font family for gutter and editor.</summary>
    public static FontFamily EditorFontFamily { get; } =
        new("Cascadia Code,Consolas,Courier New,monospace");

    /// <summary>Gets or sets the editor text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets the zoom scale (1.0 = 100%). Adjust with Ctrl+mouse wheel.</summary>
    public double ZoomScale
    {
        get => GetValue(ZoomScaleProperty);
        set => SetValue(ZoomScaleProperty, value);
    }

    /// <summary>Gets or sets whether long lines wrap in the editor.</summary>
    public bool WordWrap
    {
        get => GetValue(WordWrapProperty);
        set => SetValue(WordWrapProperty, value);
    }

    /// <summary>Gets or sets the unscaled base font size before zoom is applied.</summary>
    public double BaseFontSize
    {
        get => GetValue(BaseFontSizeProperty);
        set => SetValue(BaseFontSizeProperty, value);
    }

    /// <summary>Gets or sets the empty-editor placeholder text.</summary>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>Gets or sets the syntax highlighting profile applied to the source editor.</summary>
    public MarkdownSourceHighlightingProfile HighlightingProfile
    {
        get => GetValue(HighlightingProfileProperty);
        set => SetValue(HighlightingProfileProperty, value);
    }

    /// <summary>Raised when the editor text changes.</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>Focuses the underlying text editor.</summary>
    public void FocusEditor() => _editor.Focus();

    /// <summary>
    /// Scrolls so the caret remains inside the visible editor viewport
    /// (soft keyboard / resized host).
    /// </summary>
    public void EnsureCaretVisible()
    {
        try
        {
            _editor.TextArea.Caret.BringCaretToView();
        }
        catch
        {
            // Layout may not be ready yet during the first attach frame.
        }
    }

    /// <summary>Moves the caret to <paramref name="offset"/> and scrolls it into view.</summary>
    public void MoveCaretTo(int offset, bool ensureVisible = true)
    {
        offset = Math.Clamp(offset, 0, _editor.Document?.TextLength ?? 0);
        _editor.CaretOffset = offset;
        if (ensureVisible)
            EnsureCaretVisible();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            if (!string.Equals(_editor.Text, Text, StringComparison.Ordinal))
                _editor.Text = Text ?? string.Empty;
        }
        else if (change.Property == ZoomScaleProperty || change.Property == BaseFontSizeProperty)
        {
            UpdateTypography();
        }
        else if (change.Property == WordWrapProperty)
        {
            _editor.WordWrap = WordWrap;
            _editor.HorizontalScrollBarVisibility = WordWrap
                ? ScrollBarVisibility.Disabled
                : ScrollBarVisibility.Auto;
        }
        else if (change.Property == PlaceholderTextProperty)
        {
            _editor.Watermark = PlaceholderText ?? string.Empty;
        }
        else if (change.Property == HighlightingProfileProperty)
        {
            ApplySyntaxHighlighting();
        }
    }

    void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        HookTheme();
        ApplyThemeChrome(ResolveLight());
    }

    void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) =>
        UnhookTheme();

    void HookTheme()
    {
        if (_themeHooked || Application.Current is null)
            return;
        Application.Current.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _themeHooked = true;
    }

    void UnhookTheme()
    {
        if (!_themeHooked || Application.Current is null)
            return;
        Application.Current.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        _themeHooked = false;
    }

    void OnActualThemeVariantChanged(object? sender, EventArgs e) =>
        ApplyThemeChrome(ResolveLight());

    static bool ResolveLight()
    {
        var variant = Application.Current?.ActualThemeVariant ?? ThemeVariant.Default;
        if (variant == ThemeVariant.Light)
            return true;
        if (variant == ThemeVariant.Dark)
            return false;

        // ThemeVariant.Default — follow platform preference when available.
        try
        {
            var platform = Application.Current?.PlatformSettings;
            if (platform is not null)
                return platform.GetColorValues().ThemeVariant != PlatformThemeVariant.Dark;
        }
        catch
        {
            // Headless / incomplete platform hosts.
        }

        return true;
    }

    void ApplyThemeChrome(bool light)
    {
        if (light)
        {
            Background = new SolidColorBrush(Color.Parse("#ffffff"));
            BorderBrush = new SolidColorBrush(Color.Parse("#d0d7de"));
            _editor.Background = new SolidColorBrush(Color.Parse("#ffffff"));
            _editor.Foreground = new SolidColorBrush(Color.Parse("#1f2328"));
            _editor.LineNumbersForeground = new SolidColorBrush(Color.Parse("#656d76"));
            _editor.TextArea.CaretBrush = new SolidColorBrush(Color.Parse("#1f2328"));
            _editor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#b6e3ff"));
        }
        else
        {
            Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
            BorderBrush = new SolidColorBrush(Color.Parse("#2d2d30"));
            _editor.Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
            _editor.Foreground = new SolidColorBrush(Color.Parse("#d4d4d4"));
            _editor.LineNumbersForeground = new SolidColorBrush(Color.Parse("#6e7681"));
            _editor.TextArea.CaretBrush = new SolidColorBrush(Color.Parse("#aeafad"));
            _editor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#264f78"));
        }
    }

    void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (!string.Equals(Text, _editor.Text, StringComparison.Ordinal))
            SetCurrentValue(TextProperty, _editor.Text);

        TextChanged?.Invoke(this, new TextChangedEventArgs(null));
        EnsureCaretVisible();
    }

    void UpdateTypography()
    {
        _editor.FontSize = MarkdownZoom.ScaledFontSize(BaseFontSize, ZoomScale);
        _editor.Options.LineHeightFactor = 1.35;
    }

    void ApplySyntaxHighlighting()
    {
        _editor.SyntaxHighlighting = MarkdownSyntaxHighlighting.GetDefinition(HighlightingProfile);
    }
}
