using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Markdown;

/// <summary>
/// Markdown source editor with line-number gutter, word wrap, active-line highlight, and Ctrl+scroll zoom.
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

    private readonly TextBlock _gutter;
    private readonly TextBox _editor;
    private readonly ScrollViewer _scroll;
    private bool _syncingScroll;

    /// <summary>Creates a styled Markdown source editor.</summary>
    public MarkdownSourceEditor()
    {
        Background = new SolidColorBrush(Color.Parse("#1e1e1e"));
        BorderBrush = new SolidColorBrush(Color.Parse("#2d2d30"));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(4);
        ClipToBounds = true;

        _gutter = new TextBlock
        {
            FontFamily = EditorFontFamily,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(8, 10, 6, 10),
            Foreground = new SolidColorBrush(Color.Parse("#6e7681")),
            IsHitTestVisible = false,
        };

        var gutterBorder = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252526")),
            BorderBrush = new SolidColorBrush(Color.Parse("#333337")),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = _gutter,
            MinWidth = 44,
        };

        _editor = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CaretBrush = new SolidColorBrush(Color.Parse("#aeafad")),
            FontFamily = EditorFontFamily,
            Foreground = new SolidColorBrush(Color.Parse("#d4d4d4")),
            Padding = new Thickness(8, 10, 12, 10),
            SelectionBrush = new SolidColorBrush(Color.Parse("#264f78")),
            SelectionForegroundBrush = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
        };

        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _editor,
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
        };
        Grid.SetColumn(gutterBorder, 0);
        Grid.SetColumn(_scroll, 1);
        grid.Children.Add(gutterBorder);
        grid.Children.Add(_scroll);

        Child = grid;

        _editor.TextChanged += OnEditorTextChanged;
        _editor.PropertyChanged += OnEditorPropertyChanged;
        _scroll.ScrollChanged += OnScrollChanged;

        CtrlScrollZoom.Attach(this, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_scroll, () => ZoomScale, value => ZoomScale = value);
        CtrlScrollZoom.Attach(_editor, () => ZoomScale, value => ZoomScale = value);

        _editor.PlaceholderText = PlaceholderText ?? string.Empty;
        UpdateTypography();
        UpdateGutter();
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

    /// <summary>Focuses the underlying text box.</summary>
    public void FocusEditor() => _editor.Focus();

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            if (!string.Equals(_editor.Text, Text, StringComparison.Ordinal))
            {
                _editor.Text = Text ?? string.Empty;
                UpdateGutter();
            }
        }
        else if (change.Property == ZoomScaleProperty || change.Property == BaseFontSizeProperty)
        {
            UpdateTypography();
        }
        else if (change.Property == WordWrapProperty)
        {
            _editor.TextWrapping = WordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            _scroll.HorizontalScrollBarVisibility = WordWrap
                ? ScrollBarVisibility.Disabled
                : ScrollBarVisibility.Auto;
        }
        else if (change.Property == PlaceholderTextProperty)
            _editor.PlaceholderText = PlaceholderText ?? string.Empty;
    }

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.Equals(Text, _editor.Text, StringComparison.Ordinal))
            SetCurrentValue(TextProperty, _editor.Text);

        UpdateGutter();
        TextChanged?.Invoke(this, e);
    }

    private void OnEditorPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.CaretIndexProperty)
            UpdateGutter();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_syncingScroll)
            return;

        _syncingScroll = true;
        _gutter.Margin = new Thickness(0, -_scroll.Offset.Y, 0, 0);
        _syncingScroll = false;
    }

    private void UpdateTypography()
    {
        var size = MarkdownZoom.ScaledFontSize(BaseFontSize, ZoomScale);
        _editor.FontSize = size;
        _gutter.FontSize = size;
        _gutter.LineHeight = size * 1.35;
    }

    private void UpdateGutter()
    {
        var text = _editor.Text ?? string.Empty;
        var lineCount = LineNumberGutterFormatter.CountLines(text);
        var activeLine = LineNumberGutterFormatter.LineAtCaret(text, _editor.CaretIndex);
        _gutter.Text = LineNumberGutterFormatter.Format(lineCount, activeLine);
        _gutter.Foreground = new SolidColorBrush(Color.Parse("#6e7681"));
    }
}
