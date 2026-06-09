using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;

namespace Novolis.Avalonia.Markdown;

/// <summary>
/// Markdown source editor built on AvaloniaEdit with line numbers, word wrap, current-line highlight, and Ctrl+scroll zoom.
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

    private readonly TextEditor _editor;

    /// <summary>Creates a styled Markdown source editor.</summary>
    public MarkdownSourceEditor()
    {
        Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
        BorderBrush = new SolidColorBrush(Color.Parse("#2d2d30"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(4);
        ClipToBounds = true;

        _editor = new TextEditor
        {
            Background = new SolidColorBrush(Color.Parse("#1e1e1e")),
            Foreground = new SolidColorBrush(Color.Parse("#d4d4d4")),
            FontFamily = EditorFontFamily,
            ShowLineNumbers = true,
            WordWrap = true,
            LineNumbersForeground = new SolidColorBrush(Color.Parse("#6e7681")),
            LineNumbersMargin = new Thickness(8, 10, 6, 10),
            Padding = new Thickness(0, 10, 12, 10),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            BorderThickness = new Thickness(0),
        };

        _editor.Options.HighlightCurrentLine = true;
        _editor.Options.AcceptsTab = true;

        _editor.TextArea.CaretBrush = new SolidColorBrush(Color.Parse("#aeafad"));
        _editor.TextArea.SelectionBrush = new SolidColorBrush(Color.Parse("#264f78"));
        _editor.TextArea.SelectionForeground = Brushes.White;

        Child = _editor;

        _editor.TextChanged += OnEditorTextChanged;

        CtrlScrollZoom.Attach(this, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_editor, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_editor.TextArea, () => ZoomScale, value => ZoomScale = value);

        _editor.Watermark = PlaceholderText ?? string.Empty;
        UpdateTypography();
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

    /// <summary>Raised when the editor text changes.</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>Focuses the underlying text editor.</summary>
    public void FocusEditor() => _editor.Focus();

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
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (!string.Equals(Text, _editor.Text, StringComparison.Ordinal))
            SetCurrentValue(TextProperty, _editor.Text);

        TextChanged?.Invoke(this, new TextChangedEventArgs(null));
    }

    private void UpdateTypography()
    {
        _editor.FontSize = MarkdownZoom.ScaledFontSize(BaseFontSize, ZoomScale);
        _editor.Options.LineHeightFactor = 1.35;
    }
}
