using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Novolis.IO.Ndjson;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Ndjson;

/// <summary>Bounded Avalonia presentation for one seekable NDJSON document.</summary>
public sealed class NdjsonSliceView : Border
{
    private readonly TextBlock _documentMeta = new();
    private readonly TextBlock _rangeLabel = new();
    private readonly TextBlock _emptyLabel = new();
    private readonly Button _refreshButton;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Button _jumpButton;
    private readonly TextBox _jumpEntry;
    private readonly ComboBox _takePicker;
    private readonly StackPanel _records = new() { Spacing = 8 };
    private INdjsonDocument? _document;
    private ViewerState _state = new(0, 100, null, false, null);

    /// <summary>Creates the profile-bound NDJSON slice view.</summary>
    public NdjsonSliceView()
    {
        GraphicalProfileBinding.Bind(this, Border.BackgroundProperty, Profile.BackgroundResourceKey);
        GraphicalProfileBinding.Bind(this, Border.BorderBrushProperty, Profile.BorderResourceKey);
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(GraphicalProfileColors.CardRadius);
        Padding = new Thickness(GraphicalProfileColors.CardPadding);

        _documentMeta.Text = "No document open";
        _documentMeta.Classes.Add("ngp-body");
        _rangeLabel.Text = "No records";
        _rangeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyLabel.Text = "Open an NDJSON file to inspect its records.";
        _emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _emptyLabel.Classes.Add("ngp-body");

        _refreshButton = CreateButton("Refresh", RefreshAsync, "action-button");
        _previousButton = CreateButton("Previous", PreviousAsync, "nav-button");
        _nextButton = CreateButton("Next", NextAsync, "nav-button");
        _jumpButton = CreateButton("Jump", JumpAsync, "action-button");
        _jumpEntry = new TextBox
        {
            PlaceholderText = "Record number",
            Width = 140,
            FontFamily = Profile.BodyFont,
        };
        _takePicker = new ComboBox
        {
            ItemsSource = new[] { 50, 100, 250, 500, 1000 },
            SelectedItem = 100,
            Width = 120,
        };
        _takePicker.SelectionChanged += async (_, _) => await ChangeTakeAsync();

        var navigation = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = GraphicalProfileColors.ControlGap,
            Children =
            {
                Cell(_previousButton, 0),
                Cell(_rangeLabel, 1),
                Cell(_nextButton, 2),
            },
        };
        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = GraphicalProfileColors.ControlGap,
            Children =
            {
                new TextBlock
                {
                    Text = "Records per slice",
                    VerticalAlignment = VerticalAlignment.Center,
                },
                _takePicker,
                _jumpEntry,
                _jumpButton,
                _refreshButton,
            },
        };
        var title = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = "RECORDS",
                    Classes = { "ngp-eyebrow" },
                },
                _documentMeta,
            },
        };
        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*"),
            RowSpacing = GraphicalProfileColors.SectionGap,
            Children =
            {
                Cell(title, 0),
                Cell(navigation, 0, 1),
                Cell(controls, 0, 2),
                Cell(new ScrollViewer
                {
                    VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    Content = _records,
                }, 0, 3),
            },
        };
        Child = content;
        _previousButton.IsEnabled = false;
        _nextButton.IsEnabled = false;
        ApplyTheme();
    }

    /// <summary>The currently displayed document.</summary>
    public INdjsonDocument? Document => _document;

    /// <summary>Optional host refresh operation for URI-backed documents.</summary>
    public Func<CancellationToken, Task<INdjsonDocument?>>? RefreshDocumentAsync { get; set; }

    /// <summary>Optional host error presentation callback.</summary>
    public Func<string, Exception, Task>? ErrorHandler { get; set; }

    /// <summary>Opens a document and loads the current slice.</summary>
    public async Task OpenAsync(
        INdjsonDocument document,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        _state = _state with { Skip = 0, Slice = null, Error = null };
        _documentMeta.Text = displayName ?? "Loading...";
        SetBusy(true);
        try
        {
            await LoadSliceAsync(cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            _state = _state with { Error = error };
            await ReportErrorAsync("Unable to read NDJSON", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Applies profile brushes to code-created chrome.</summary>
    public void ApplyTheme()
    {
        GraphicalProfileBinding.Bind(this, Border.BackgroundProperty, Profile.BackgroundResourceKey);
        GraphicalProfileBinding.Bind(this, Border.BorderBrushProperty, Profile.BorderResourceKey);
        _documentMeta.Foreground = Profile.MutedBrush;
        _rangeLabel.Foreground = Profile.TextBrush;
        _emptyLabel.Foreground = Profile.MutedBrush;
    }

    private async Task RefreshAsync()
    {
        if (_document is null)
            return;

        _state = ViewerNavigation.BeginRefresh(_state);
        SetBusy(true);
        try
        {
            if (RefreshDocumentAsync is { } refresh)
                _document = await refresh(CancellationToken.None) ?? _document;
            else
                await _document.RefreshAsync();

            await LoadSliceAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            _state = _state with { Error = error };
            await ReportErrorAsync("Unable to refresh NDJSON", error);
        }
        finally
        {
            _state = ViewerNavigation.CompleteRefresh(_state);
            SetBusy(false);
        }
    }

    private async Task PreviousAsync()
    {
        if (_document is null)
            return;

        _state = ViewerNavigation.Previous(_state);
        await TryLoadSliceAsync();
    }

    private async Task NextAsync()
    {
        if (_document is null)
            return;

        _state = ViewerNavigation.Next(_state);
        await TryLoadSliceAsync();
    }

    private async Task JumpAsync()
    {
        if (!long.TryParse(_jumpEntry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var skip)
            || skip < 0)
        {
            await ReportErrorAsync(
                "Jump to record",
                new ArgumentException("Enter a non-negative record number."));
            return;
        }

        _state = ViewerNavigation.Jump(_state, skip);
        await TryLoadSliceAsync();
    }

    private async Task ChangeTakeAsync()
    {
        if (_takePicker.SelectedItem is not int take)
            return;

        _state = ViewerNavigation.ChangeTake(_state, take);
        await TryLoadSliceAsync();
    }

    private async Task LoadSliceAsync(CancellationToken cancellationToken)
    {
        if (_document is not { } document)
            return;

        var slice = await document.ReadAsync(_state.Skip, _state.Take, cancellationToken);
        _state = _state with { Slice = slice, Error = null, IsRefreshing = false };
        RenderSlice(document);
    }

    private async Task TryLoadSliceAsync()
    {
        try
        {
            SetBusy(true);
            await LoadSliceAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            _state = _state with { Error = error };
            await ReportErrorAsync("Unable to read NDJSON", error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RenderSlice(INdjsonDocument document)
    {
        _records.Children.Clear();
        if (_state.Slice is not { Records.Count: > 0 } slice)
        {
            _emptyLabel.IsVisible = true;
            _records.Children.Add(_emptyLabel);
        }
        else
        {
            _emptyLabel.IsVisible = false;
            foreach (var record in slice.Records)
                _records.Children.Add(CreateRecordRow(new NdjsonRecordDisplay(record)));
        }

        var first = _state.Slice?.Records.FirstOrDefault();
        var last = _state.Slice?.Records.LastOrDefault();
        _rangeLabel.Text = first is null || last is null
            ? $"{_state.Skip:N0} · no records"
            : $"{first.Number:N0} – {last.Number:N0}";
        document.File.Refresh();
        _documentMeta.Text = $"{FormatBytes(document.File.Length)} · {document.RecordCount:N0}"
            + (_state.Slice?.HasMore is true ? "+" : string.Empty)
            + " complete records";
        _previousButton.IsEnabled = _state.Slice?.HasPrevious is true;
        _nextButton.IsEnabled = _state.Slice?.HasMore is true;
        ApplyTheme();
    }

    private Control CreateRecordRow(NdjsonRecordDisplay record)
    {
        var details = new TextBlock
        {
            IsVisible = false,
            FontFamily = Profile.MonoFont,
            FontSize = GraphicalProfileColors.DebugSize,
            TextWrapping = TextWrapping.Wrap,
        };
        Button? expand = null;
        expand = CreateButton("Expand", async () =>
        {
            if (!details.IsVisible)
                details.Text = await Task.Run(() => record.Details);

            details.IsVisible = !details.IsVisible;
            if (expand is { } button)
                button.Content = details.IsVisible ? "Collapse" : "Expand";
        }, "nav-button");
        var copy = CreateButton("Copy", async () =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(record.CopyText);
        }, "nav-button");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { expand, copy },
        };
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 8,
            Children =
            {
                Cell(new TextBlock
                {
                    Text = record.Number.ToString("N0", CultureInfo.InvariantCulture),
                    FontFamily = Profile.MonoFont,
                    FontWeight = FontWeight.Bold,
                }, 0),
                Cell(new TextBlock
                {
                    Text = record.Status,
                    Foreground = Profile.AccentBrush,
                    FontWeight = FontWeight.Bold,
                }, 1),
                Cell(actions, 2),
            },
        };
        var row = new Border
        {
            Classes = { "ngp-card" },
            Padding = new Thickness(12, 10),
            Child = new StackPanel
            {
                Spacing = 7,
                Children =
                {
                    header,
                    new TextBlock
                    {
                        Text = record.Preview,
                        FontFamily = Profile.MonoFont,
                        FontSize = GraphicalProfileColors.DebugSize,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    details,
                },
            },
        };
        return row;
    }

    private async Task ReportErrorAsync(string title, Exception error)
    {
        if (ErrorHandler is { } handler)
            await handler(title, error);
    }

    private void SetBusy(bool isBusy)
    {
        _refreshButton.IsEnabled = !isBusy && _document is not null;
        _previousButton.IsEnabled = !isBusy && _state.Slice?.HasPrevious is true;
        _nextButton.IsEnabled = !isBusy && _state.Slice?.HasMore is true;
        _jumpButton.IsEnabled = !isBusy;
        _takePicker.IsEnabled = !isBusy;
    }

    private static Button CreateButton(
        string text,
        Func<Task> action,
        string className)
    {
        var button = new Button
        {
            Content = text,
            FontFamily = Profile.BodyFont,
        };
        button.Classes.Add(className);
        button.Click += async (_, _) => await action();
        return button;
    }

    private static T Cell<T>(T control, int column, int row = 0)
        where T : Control
    {
        Grid.SetColumn(control, column);
        Grid.SetRow(control, row);
        return control;
    }

    private static string FormatBytes(long bytes)
    {
        var value = (double)Math.Max(0, bytes);
        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private sealed class NdjsonRecordDisplay
    {
        private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

        public NdjsonRecordDisplay(NdjsonRecord record)
        {
            Record = record;
            Number = record.Number;
            Status = record.IsValid ? "VALID" : "MALFORMED";
            var preview = record.Json is { } json ? json.GetRawText() : record.Raw ?? string.Empty;
            Preview = preview.Length <= 4_096 ? preview : $"{preview[..4_096]}…";
            CopyText = record.Json is { } copy ? copy.GetRawText() : record.Raw ?? string.Empty;
        }

        public long Number { get; }
        public string Status { get; }
        public string Preview { get; }
        public string Details => _details ??= Record.Json is { } value
            ? JsonSerializer.Serialize(value, PrettyJson)
            : $"{Record.Raw ?? string.Empty}\n\n{Record.Error?.Message}";
        public string CopyText { get; }

        private string? _details;
        private NdjsonRecord Record { get; }
    }
}
