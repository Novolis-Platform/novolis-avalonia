using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
namespace Novolis.Avalonia.Markdown;

/// <summary>Split-pane Markdown studio with independent editor/preview zoom and debounced live preview.</summary>
public sealed class MarkdownEditorPreview : Grid
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarkdownEditorPreview, string?>(nameof(Text), string.Empty);

    public static readonly StyledProperty<double> EditorZoomScaleProperty =
        AvaloniaProperty.Register<MarkdownEditorPreview, double>(nameof(EditorZoomScale), MarkdownZoom.Default);

    public static readonly StyledProperty<double> PreviewZoomScaleProperty =
        AvaloniaProperty.Register<MarkdownEditorPreview, double>(nameof(PreviewZoomScale), MarkdownZoom.Default);

    public static readonly StyledProperty<MarkdownPreviewTheme> PreviewThemeProperty =
        AvaloniaProperty.Register<MarkdownEditorPreview, MarkdownPreviewTheme>(nameof(PreviewTheme), MarkdownPreviewTheme.StudioDark);

    public static readonly StyledProperty<int> PreviewRefreshDelayMillisecondsProperty =
        AvaloniaProperty.Register<MarkdownEditorPreview, int>(nameof(PreviewRefreshDelayMilliseconds), 250);

    public static readonly StyledProperty<double> SplitterWidthProperty =
        AvaloniaProperty.Register<MarkdownEditorPreview, double>(nameof(SplitterWidth), 5);

    private readonly MarkdownSourceEditor _editor;
    private readonly MarkdownPreviewPane _preview;
    private readonly GridSplitter _splitter;
    private readonly DispatcherTimer _previewTimer;
    private bool _suppressTextSync;

    /// <summary>Creates a split editor and preview workspace.</summary>
    public MarkdownEditorPreview()
    {
        ColumnDefinitions = new ColumnDefinitions("*,Auto,*");
        MinHeight = 200;

        _editor = new MarkdownSourceEditor();
        _preview = new MarkdownPreviewPane();
        _splitter = new GridSplitter
        {
            Width = SplitterWidth,
            Background = new SolidColorBrush(Color.Parse("#3a3a3a")),
            ResizeDirection = GridResizeDirection.Columns,
        };

        Grid.SetColumn(_editor, 0);
        Grid.SetColumn(_splitter, 1);
        Grid.SetColumn(_preview, 2);
        Children.Add(_editor);
        Children.Add(_splitter);
        Children.Add(_preview);

        _editor.TextChanged += OnEditorTextChanged;
        _editor.PropertyChanged += OnChildPropertyChanged;
        _preview.PropertyChanged += OnChildPropertyChanged;

        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(PreviewRefreshDelayMilliseconds),
        };
        _previewTimer.Tick += (_, _) =>
        {
            _previewTimer.Stop();
            _preview.Markdown = Text;
        };
    }

    /// <summary>Gets or sets the shared Markdown document text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets editor zoom (Ctrl+scroll over the editor).</summary>
    public double EditorZoomScale
    {
        get => GetValue(EditorZoomScaleProperty);
        set => SetValue(EditorZoomScaleProperty, value);
    }

    /// <summary>Gets or sets preview zoom (Ctrl+scroll over the preview).</summary>
    public double PreviewZoomScale
    {
        get => GetValue(PreviewZoomScaleProperty);
        set => SetValue(PreviewZoomScaleProperty, value);
    }

    /// <summary>Gets or sets the preview HTML theme.</summary>
    public MarkdownPreviewTheme PreviewTheme
    {
        get => GetValue(PreviewThemeProperty);
        set => SetValue(PreviewThemeProperty, value);
    }

    /// <summary>Gets or sets debounce delay before refreshing the preview after edits.</summary>
    public int PreviewRefreshDelayMilliseconds
    {
        get => GetValue(PreviewRefreshDelayMillisecondsProperty);
        set => SetValue(PreviewRefreshDelayMillisecondsProperty, value);
    }

    /// <summary>Gets or sets the column splitter width in pixels.</summary>
    public double SplitterWidth
    {
        get => GetValue(SplitterWidthProperty);
        set => SetValue(SplitterWidthProperty, value);
    }

    /// <summary>Gets the source editor control.</summary>
    public MarkdownSourceEditor Editor => _editor;

    /// <summary>Gets the preview pane control.</summary>
    public MarkdownPreviewPane Preview => _preview;

    /// <summary>Raised when the document text changes.</summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>Focuses the source editor.</summary>
    public void FocusEditor() => _editor.FocusEditor();

    /// <summary>Immediately refreshes the preview without waiting for debounce.</summary>
    public void RefreshPreviewNow()
    {
        _previewTimer.Stop();
        _preview.Markdown = Text;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            if (!_suppressTextSync && !string.Equals(_editor.Text, Text, StringComparison.Ordinal))
            {
                _suppressTextSync = true;
                _editor.Text = Text;
                _suppressTextSync = false;
            }

            SchedulePreviewRefresh();
        }
        else if (change.Property == EditorZoomScaleProperty)
        {
            if (Math.Abs(_editor.ZoomScale - EditorZoomScale) > 0.0001)
                _editor.ZoomScale = EditorZoomScale;
        }
        else if (change.Property == PreviewZoomScaleProperty)
        {
            if (Math.Abs(_preview.ZoomScale - PreviewZoomScale) > 0.0001)
                _preview.ZoomScale = PreviewZoomScale;
        }
        else if (change.Property == PreviewThemeProperty)
            _preview.PreviewTheme = PreviewTheme;
        else if (change.Property == PreviewRefreshDelayMillisecondsProperty)
            _previewTimer.Interval = TimeSpan.FromMilliseconds(PreviewRefreshDelayMilliseconds);
        else if (change.Property == SplitterWidthProperty)
            _splitter.Width = SplitterWidth;
    }

    private void OnChildPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MarkdownSourceEditor.ZoomScaleProperty)
            SetCurrentValue(EditorZoomScaleProperty, _editor.ZoomScale);
        else if (e.Property == MarkdownPreviewPane.ZoomScaleProperty)
            SetCurrentValue(PreviewZoomScaleProperty, _preview.ZoomScale);
    }

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextSync)
            return;

        _suppressTextSync = true;
        SetCurrentValue(TextProperty, _editor.Text);
        _suppressTextSync = false;

        SchedulePreviewRefresh();
        TextChanged?.Invoke(this, e);
    }

    private void SchedulePreviewRefresh()
    {
        _previewTimer.Stop();
        _previewTimer.Interval = TimeSpan.FromMilliseconds(PreviewRefreshDelayMilliseconds);
        _previewTimer.Start();
    }
}
