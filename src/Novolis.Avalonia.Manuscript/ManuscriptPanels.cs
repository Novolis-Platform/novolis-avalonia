using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Manuscript;

namespace Novolis.Avalonia.Manuscript;

/// <summary>Composable chapter list pane (no workspace I/O).</summary>
public sealed class ChapterListPane : UserControl
{
    readonly ListBox _list = new()
    {
        SelectionMode = SelectionMode.Single,
    };

    readonly List<ChapterInfo> _chapters = [];

    /// <summary>Raised when the user selects a chapter.</summary>
    public event EventHandler<ChapterInfo?>? ChapterSelected;

    /// <summary>Creates an empty chapter list pane.</summary>
    public ChapterListPane()
    {
        Content = _list;
        _list.SelectionChanged += (_, _) =>
        {
            ChapterSelected?.Invoke(this, _list.SelectedItem as ChapterInfo);
        };
    }

    /// <summary>Rebinds the list from catalog chapters.</summary>
    public void SetChapters(IReadOnlyList<ChapterInfo> chapters)
    {
        _chapters.Clear();
        _chapters.AddRange(chapters);
        _list.ItemsSource = null;
        _list.ItemsSource = _chapters.Select(c => new ChapterListItem(c, ChapterListFormatting.FormatLabel(c))).ToList();
        _list.SelectionChanged -= OnWrappedSelection;
        _list.SelectionChanged += OnWrappedSelection;
    }

    void OnWrappedSelection(object? sender, SelectionChangedEventArgs e)
    {
        var item = _list.SelectedItem as ChapterListItem;
        ChapterSelected?.Invoke(this, item?.Chapter);
    }

    sealed record ChapterListItem(ChapterInfo Chapter, string Label)
    {
        public override string ToString() => Label;
    }
}

/// <summary>Composable metadata form pane bound to <see cref="ManuscriptChapterMetadata"/>.</summary>
public sealed class MetadataFormPane : UserControl
{
    readonly TextBox _number = Field("Number");
    readonly TextBox _title = Field("Title");
    readonly TextBox _date = Field("Date");
    readonly TextBox _time = Field("Time");
    readonly TextBox _system = Field("System");
    readonly TextBox _location = Field("Location");
    readonly TextBox _pov = Field("POV");
    readonly TextBox _characters = Field("Characters");
    readonly TextBox _status = Field("Status");
    readonly TextBox _notes = Field("Notes");
    readonly Button _apply = new() { Content = "Apply metadata", HorizontalAlignment = HorizontalAlignment.Stretch };

    /// <summary>Raised when Apply is clicked with the edited metadata.</summary>
    public event EventHandler<ManuscriptChapterMetadata>? ApplyRequested;

    /// <summary>Creates the metadata form.</summary>
    public MetadataFormPane()
    {
        var stack = new StackPanel { Spacing = 6, Margin = new Thickness(8) };
        foreach (var box in new[] { _number, _title, _date, _time, _system, _location, _pov, _characters, _status, _notes })
            stack.Children.Add(Labeled(box));
        stack.Children.Add(_apply);
        _apply.Click += (_, _) => ApplyRequested?.Invoke(this, Read());
        Content = new ScrollViewer { Content = stack };
    }

    /// <summary>Loads fields from metadata.</summary>
    public void Load(ManuscriptChapterMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        _number.Text = meta.Number ?? "";
        _title.Text = meta.Title ?? "";
        _date.Text = meta.Date ?? "";
        _time.Text = meta.Time ?? "";
        _system.Text = meta.System ?? "";
        _location.Text = meta.Location ?? "";
        _pov.Text = meta.Pov ?? "";
        _characters.Text = meta.Characters ?? "";
        _status.Text = meta.Status ?? "";
        _notes.Text = meta.Notes ?? "";
    }

    /// <summary>Reads current field values into a metadata object.</summary>
    public ManuscriptChapterMetadata Read() => new()
    {
        Number = NullIfEmpty(_number.Text),
        Title = NullIfEmpty(_title.Text),
        Date = NullIfEmpty(_date.Text),
        Time = NullIfEmpty(_time.Text),
        System = NullIfEmpty(_system.Text),
        Location = NullIfEmpty(_location.Text),
        Pov = NullIfEmpty(_pov.Text),
        Characters = NullIfEmpty(_characters.Text),
        Status = NullIfEmpty(_status.Text),
        Notes = NullIfEmpty(_notes.Text),
    };

    static TextBox Field(string placeholder) => new() { PlaceholderText = placeholder };
    static Control Labeled(TextBox box) => box;
    static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>Read-only diagnostics list bound to <see cref="DiagnosticFinding"/> rows.</summary>
public sealed class DiagnosticsListPane : UserControl
{
    readonly ListBox _list = new();

    /// <summary>Creates an empty diagnostics pane.</summary>
    public DiagnosticsListPane()
    {
        Content = _list;
    }

    /// <summary>Rebinds findings.</summary>
    public void SetFindings(IReadOnlyList<DiagnosticFinding> findings)
    {
        _list.ItemsSource = findings
            .Select(f => $"{f.Severity}: [{f.Code}] {f.Message}" + (string.IsNullOrWhiteSpace(f.Path) ? "" : $" ({f.Path})"))
            .ToList();
    }
}
