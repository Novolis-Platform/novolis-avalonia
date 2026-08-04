using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Catalog;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>
/// Browse curated free collections, paste commercial inspiration URLs (Artlist, …),
/// download allowed assets, and run explore transformers.
/// </summary>
public sealed class MediaCatalogWorkspace : Border
{
    readonly MediaCatalogHub _hub;
    readonly ListBox _collections = new();
    readonly ListBox _items = new();
    readonly TextBox _filter = new();
    readonly TextBox _inspireUrl = new();
    readonly StackPanel _transformChecks = new() { Spacing = 4 };
    readonly TextBlock _detail = new();
    readonly TextBlock _status = new();
    readonly Dictionary<string, CheckBox> _transformBoxes = new(StringComparer.OrdinalIgnoreCase);

    List<MediaCollection> _allCollections = [];
    MediaCollection? _selectedCollection;
    MediaItem? _selectedItem;

    public MediaCatalogWorkspace(MediaCatalogHub? hub = null)
    {
        _hub = hub ?? MediaCatalogHub.CreateDefault();
        Background = AudioEditPalette.Pane;
        BorderBrush = AudioEditPalette.Border;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(10);

        foreach (var step in _hub.Pipeline.Steps)
        {
            var box = new CheckBox
            {
                Content = step.DisplayName,
                IsChecked = true,
                Foreground = Brushes.White,
                Tag = step.Id,
            };
            ToolTip.SetTip(box, step.Description);
            _transformBoxes[step.Id] = box;
            _transformChecks.Children.Add(box);
        }

        Child = BuildLayout();
        _ = ReloadAsync();
    }

    /// <summary>Underlying hub (sources, cache, pipeline).</summary>
    public MediaCatalogHub Hub => _hub;

    /// <summary>Raised when a transformer run produces a <see cref="MusicScore"/>.</summary>
    public event Action<MusicScore>? ScoreProduced;

    /// <summary>Raised after a successful download (local path).</summary>
    public event Action<MediaItem, string>? ItemDownloaded;

    /// <summary>Optional host hook after explore completes.</summary>
    public event Action<MediaTransformContext>? ExploreCompleted;

    Grid BuildLayout()
    {
        _collections.Background = AudioEditPalette.PaneAlt;
        _collections.Foreground = Brushes.White;
        _collections.SelectionChanged += (_, _) => OnCollectionSelected();

        _items.Background = AudioEditPalette.PaneAlt;
        _items.Foreground = Brushes.White;
        _items.SelectionChanged += (_, _) => OnItemSelected();

        StyleText(_filter, "Filter title / tags…");
        StyleText(_inspireUrl, "Paste Artlist (or similar) collection URL…");
        _detail.Foreground = Brushes.WhiteSmoke;
        _detail.TextWrapping = TextWrapping.Wrap;
        _detail.FontSize = 12;
        _status.Foreground = AudioEditPalette.Amber;
        _status.TextWrapping = TextWrapping.Wrap;
        _status.FontSize = 12;
        _status.Text = "Ready — commercial catalogs are inspiration-only.";

        var inspireRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(0, 0, 0, 8),
        };
        inspireRow.Children.Add(_inspireUrl);
        Grid.SetColumn(_inspireUrl, 0);
        var mapBtn = MakeButton("Map inspiration", async () => await MapInspirationAsync());
        Grid.SetColumn(mapBtn, 1);
        inspireRow.Children.Add(mapBtn);
        var openBtn = MakeButton("Open URL", OpenSelectedInspiration);
        Grid.SetColumn(openBtn, 2);
        inspireRow.Children.Add(openBtn);

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                MakeButton("Download", async () => await RunTransformersAsync(downloadOnly: true)),
                MakeButton("Run transformers", async () => await RunTransformersAsync(downloadOnly: false)),
                MakeButton("Refresh", async () => await ReloadAsync()),
            },
        };

        var left = new DockPanel { Margin = new Thickness(0, 0, 8, 0) };
        var leftHeader = MakeHeader("Collections");
        DockPanel.SetDock(leftHeader, Dock.Top);
        left.Children.Add(leftHeader);
        left.Children.Add(_collections);

        var center = new DockPanel { Margin = new Thickness(0, 0, 8, 0) };
        var filterBar = new StackPanel { Spacing = 6 };
        filterBar.Children.Add(MakeHeader("Items"));
        filterBar.Children.Add(_filter);
        _filter.TextChanged += (_, _) => RefreshItems();
        DockPanel.SetDock(filterBar, Dock.Top);
        center.Children.Add(filterBar);
        center.Children.Add(_items);

        var right = new DockPanel();
        var rightTop = new StackPanel { Spacing = 8 };
        rightTop.Children.Add(MakeHeader("Explore"));
        rightTop.Children.Add(inspireRow);
        rightTop.Children.Add(MakeHeader("Transformers"));
        rightTop.Children.Add(_transformChecks);
        rightTop.Children.Add(actionRow);
        rightTop.Children.Add(_detail);
        rightTop.Children.Add(_status);
        DockPanel.SetDock(rightTop, Dock.Top);
        right.Children.Add(rightTop);

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("220,*,320"),
            RowDefinitions = new RowDefinitions("*"),
        };
        root.Children.Add(left);
        Grid.SetColumn(left, 0);
        root.Children.Add(center);
        Grid.SetColumn(center, 1);
        root.Children.Add(right);
        Grid.SetColumn(right, 2);
        return root;
    }

    static TextBlock MakeHeader(string text) => new()
    {
        Text = text,
        FontSize = 14,
        FontWeight = FontWeight.SemiBold,
        Foreground = AudioEditPalette.Accent,
        Margin = new Thickness(0, 0, 0, 4),
    };

    static void StyleText(TextBox box, string watermark)
    {
        box.PlaceholderText = watermark;
        box.Background = AudioEditPalette.PaneAlt;
        box.Foreground = Brushes.White;
        box.BorderBrush = AudioEditPalette.Border;
    }

    Button MakeButton(string label, Func<Task> onClick)
    {
        var b = new Button
        {
            Content = label,
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(10, 6),
            Background = AudioEditPalette.PaneAlt,
            Foreground = Brushes.White,
            BorderBrush = AudioEditPalette.Border,
        };
        b.Click += async (_, _) =>
        {
            try
            {
                await onClick();
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        };
        return b;
    }

    Button MakeButton(string label, Action onClick)
    {
        var b = new Button
        {
            Content = label,
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(10, 6),
            Background = AudioEditPalette.PaneAlt,
            Foreground = Brushes.White,
            BorderBrush = AudioEditPalette.Border,
        };
        b.Click += (_, _) =>
        {
            try
            {
                onClick();
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        };
        return b;
    }

    async Task ReloadAsync()
    {
        _allCollections = (await _hub.ListAllCollectionsAsync()).ToList();
        _collections.ItemsSource = _allCollections
            .Select(c => $"{c.Title}  ({c.Count})" + (c.Access == MediaAccessMode.InspirationOnly ? " · inspire" : ""))
            .ToList();

        var prefer = _allCollections.FindIndex(c => c.Id == "inspired-cinematic-space");
        _collections.SelectedIndex = prefer >= 0 ? prefer : 0;
        _status.Text = $"{_allCollections.Count} collections · cache {_hub.Cache.RootDirectory}";
    }

    void OnCollectionSelected()
    {
        var idx = _collections.SelectedIndex;
        _selectedCollection = idx >= 0 && idx < _allCollections.Count ? _allCollections[idx] : null;
        RefreshItems();
        if (_selectedCollection?.InspirationUri is { } uri)
            _inspireUrl.Text = uri.ToString();
    }

    void RefreshItems()
    {
        if (_selectedCollection is null)
        {
            _items.ItemsSource = Array.Empty<string>();
            return;
        }

        var q = _filter.Text?.Trim();
        IEnumerable<MediaItem> items = _selectedCollection.Items;
        if (!string.IsNullOrWhiteSpace(q))
        {
            items = items.Where(i =>
                i.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || i.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        var list = items.ToList();
        _items.ItemsSource = list.Select(FormatItem).ToList();
        _items.Tag = list;
        if (list.Count > 0)
            _items.SelectedIndex = 0;
    }

    static string FormatItem(MediaItem i) =>
        $"{(i.Kind == MediaKind.Midi ? "MIDI" : "Audio")} · {i.Title}";

    void OnItemSelected()
    {
        if (_items.Tag is not List<MediaItem> list)
            return;
        var idx = _items.SelectedIndex;
        _selectedItem = idx >= 0 && idx < list.Count ? list[idx] : null;
        if (_selectedItem is null)
        {
            _detail.Text = "";
            return;
        }

        var i = _selectedItem;
        _detail.Text =
            $"{i.Title}\n{i.ArtistOrSource}\nLicense: {i.License.Name}\n" +
            $"Download: {(i.CanDownload ? "allowed" : "blocked / inspiration")}\n" +
            $"Tags: {string.Join(", ", i.Tags)}\n" +
            (i.Notes ?? "") +
            (string.IsNullOrEmpty(i.DownloadUrl) ? "" : $"\nURL: {i.DownloadUrl}");

        foreach (var (id, box) in _transformBoxes)
        {
            var step = _hub.Pipeline.Steps.FirstOrDefault(s => s.Id == id);
            box.IsEnabled = step is not null && _selectedItem is not null && step.AppliesTo(_selectedItem);
            if (!box.IsEnabled)
                box.IsChecked = false;
            else if (box.IsChecked != true && step is not null)
                box.IsChecked = true;
        }
    }

    async Task MapInspirationAsync()
    {
        var text = _inspireUrl.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            _status.Text = "Paste a valid https URL first.";
            return;
        }

        if (!MediaDownloadPolicy.LooksLikeCommercialInspiration(text))
        {
            _status.Text = "That host allows download — add it as a curated free entry instead of inspiration.";
            return;
        }

        var (bookmark, standIn) = _hub.AddInspiration(uri);
        await ReloadAsync();
        if (standIn is not null)
        {
            var idx = _allCollections.FindIndex(c => c.Id == standIn.Id);
            if (idx >= 0)
                _collections.SelectedIndex = idx;
            _status.Text =
                $"Bookmarked {bookmark.Title}. Showing free stand-in “{standIn.Title}”. " +
                "Artlist/etc. files are never downloaded.";
        }
        else
        {
            _status.Text = $"Bookmarked {bookmark.Title} (no free stand-in mapped).";
        }
    }

    void OpenSelectedInspiration()
    {
        var url = _selectedCollection?.InspirationUri?.ToString()
                  ?? _selectedItem?.DownloadUrl
                  ?? _inspireUrl.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            _status.Text = "No URL to open.";
            return;
        }

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        _status.Text = "Opened in browser.";
    }

    async Task RunTransformersAsync(bool downloadOnly)
    {
        if (_selectedItem is null)
        {
            _status.Text = "Select an item.";
            return;
        }

        if (!_selectedItem.CanDownload && downloadOnly)
        {
            _status.Text = "Item is inspiration-only — open URL or switch to a free stand-in collection.";
            return;
        }

        IEnumerable<string>? ids = downloadOnly
            ? ["download"]
            : _transformBoxes.Where(kv => kv.Value.IsChecked == true).Select(kv => kv.Key);

        _status.Text = "Running…";
        var ctx = await _hub.ExploreAsync(_selectedItem, ids);
        if (ctx.LocalPath is not null)
            ItemDownloaded?.Invoke(_selectedItem, ctx.LocalPath);
        if (ctx.Score is not null)
            ScoreProduced?.Invoke(ctx.Score);
        ExploreCompleted?.Invoke(ctx);

        _status.Text = ctx.Ok
            ? $"OK · path={ctx.LocalPath ?? "(none)"} · score={(ctx.Score is null ? "no" : ctx.Score.Title)}"
            : string.Join("; ", ctx.Errors);
        if (ctx.Log.Count > 0)
            _detail.Text = _detail.Text + "\n\n" + string.Join("\n", ctx.Log);
    }
}
