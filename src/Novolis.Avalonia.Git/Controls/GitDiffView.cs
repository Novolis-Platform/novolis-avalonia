using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Novolis.IO.Git;
using ScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility;

namespace Novolis.Avalonia.Git;

/// <summary>Professional unified-diff viewer (file list + colored hunks + line gutters).</summary>
public sealed class GitDiffView : UserControl
{
    static readonly FontFamily Mono = new("Cascadia Mono, Consolas, Cascadia Code, monospace");
    static readonly Regex HunkRx = new(
        @"^@@\s+-(\d+)(?:,\d+)?\s+\+(\d+)(?:,\d+)?\s*@@",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static readonly IBrush ShellBg = new ImmutableSolidColorBrush(Color.FromRgb(18, 20, 24));
    static readonly IBrush PaneBg = new ImmutableSolidColorBrush(Color.FromRgb(28, 30, 36));
    static readonly IBrush ChromeBorder = new ImmutableSolidColorBrush(Color.FromRgb(52, 56, 64));
    static readonly IBrush Muted = new ImmutableSolidColorBrush(Color.FromRgb(140, 148, 160));
    static readonly IBrush TextBrush = new ImmutableSolidColorBrush(Color.FromRgb(230, 234, 240));
    static readonly IBrush HunkBg = new ImmutableSolidColorBrush(Color.FromRgb(36, 48, 68));
    static readonly IBrush HunkFg = new ImmutableSolidColorBrush(Color.FromRgb(140, 170, 210));
    static readonly IBrush AddBg = new ImmutableSolidColorBrush(Color.FromRgb(28, 58, 42));
    static readonly IBrush AddFg = new ImmutableSolidColorBrush(Color.FromRgb(170, 230, 190));
    static readonly IBrush DelBg = new ImmutableSolidColorBrush(Color.FromRgb(72, 32, 38));
    static readonly IBrush DelFg = new ImmutableSolidColorBrush(Color.FromRgb(255, 180, 180));
    static readonly IBrush AddBadge = new ImmutableSolidColorBrush(Color.FromRgb(46, 160, 90));
    static readonly IBrush DelBadge = new ImmutableSolidColorBrush(Color.FromRgb(220, 80, 90));
    static readonly IBrush GutterBg = new ImmutableSolidColorBrush(Color.FromRgb(24, 26, 32));
    static readonly IBrush HeaderBg = new ImmutableSolidColorBrush(Color.FromRgb(34, 38, 46));

    readonly ListBox _files = new() { SelectionMode = SelectionMode.Single };
    readonly TextBlock _title = new()
    {
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.NoWrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
        VerticalAlignment = VerticalAlignment.Center,
    };
    readonly TextBlock _stats = new()
    {
        FontFamily = Mono,
        FontSize = 11,
        Foreground = Muted,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(10, 0, 0, 0),
    };
    readonly ItemsControl _lines = new();
    readonly TextBlock _empty = new()
    {
        Text = "Select a commit to inspect its diff.",
        Foreground = Muted,
        Margin = new Thickness(16),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };
    readonly Grid _viewer = new();
    DiffDocument? _doc;

    /// <summary>Creates the viewer.</summary>
    public GitDiffView()
    {
        _files.ItemTemplate = new FuncDataTemplate<FileVm>((vm, _) => BuildFileRow(vm), true);
        _files.SelectionChanged += (_, _) => ShowSelectedFile();
        _lines.ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());
        _lines.ItemTemplate = new FuncDataTemplate<LineVm>((vm, _) => BuildLineRow(vm), true);

        var fileHeader = new TextBlock
        {
            Text = "Files",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(10, 8, 10, 6),
        };
        var filePane = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(fileHeader, Dock.Top);
        filePane.Children.Add(fileHeader);
        filePane.Children.Add(_files);

        var titleBar = new Border
        {
            Background = HeaderBg,
            BorderBrush = ChromeBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 8),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children = { _stats, _title },
            },
        };
        DockPanel.SetDock(_stats, Dock.Right);

        var lineScroll = new ScrollViewer
        {
            Content = _lines,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = ShellBg,
        };

        var right = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(titleBar, Dock.Top);
        right.Children.Add(titleBar);
        right.Children.Add(lineScroll);

        _viewer.ColumnDefinitions = new ColumnDefinitions("220,1,*");
        var leftBorder = new Border
        {
            Background = PaneBg,
            BorderBrush = ChromeBorder,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = filePane,
        };
        var splitter = new Border { Background = ChromeBorder, Width = 1 };
        Grid.SetColumn(leftBorder, 0);
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(right, 2);
        _viewer.Children.Add(leftBorder);
        _viewer.Children.Add(splitter);
        _viewer.Children.Add(right);

        var root = new Grid();
        root.Children.Add(_empty);
        root.Children.Add(_viewer);
        _viewer.IsVisible = false;
        Content = root;
        Background = ShellBg;
    }

    /// <summary>Binds a parsed diff document.</summary>
    public void SetDiff(DiffDocument? doc)
    {
        _doc = doc;
        if (doc is null || doc.Files.Count == 0)
        {
            _viewer.IsVisible = false;
            _empty.IsVisible = true;
            _empty.Text = doc is null ? "Select a commit to inspect its diff." : "No textual changes in this commit.";
            _files.ItemsSource = null;
            _lines.ItemsSource = null;
            return;
        }

        _empty.IsVisible = false;
        _viewer.IsVisible = true;
        var files = doc.Files.Select(FileVm.From).ToList();
        _files.ItemsSource = files;
        _files.SelectedIndex = 0;
    }

    void ShowSelectedFile()
    {
        if (_files.SelectedItem is not FileVm file)
        {
            _title.Text = "";
            _stats.Text = "";
            _lines.ItemsSource = null;
            return;
        }

        _title.Text = file.DisplayPath;
        _stats.Text = file.IsBinary
            ? "binary"
            : $"+{file.Added}  −{file.Deleted}  ·  {file.HunkCount} hunk{(file.HunkCount == 1 ? "" : "s")}";
        _lines.ItemsSource = file.IsBinary
            ? new[] { LineVm.Meta("Binary file — contents not shown.") }
            : BuildLines(file.Source).ToList();
    }

    static IEnumerable<LineVm> BuildLines(DiffFile file)
    {
        foreach (var hunk in file.Hunks)
        {
            var (oldLine, newLine) = ParseHunkStart(hunk.Header);
            yield return LineVm.Hunk(hunk.Header);
            foreach (var line in hunk.Lines)
            {
                switch (line.Kind)
                {
                    case '+':
                        yield return LineVm.Add(newLine, line.Text);
                        newLine++;
                        break;
                    case '-':
                        yield return LineVm.Del(oldLine, line.Text);
                        oldLine++;
                        break;
                    default:
                        yield return LineVm.Context(oldLine, newLine, line.Text);
                        oldLine++;
                        newLine++;
                        break;
                }
            }
        }
    }

    static (int Old, int New) ParseHunkStart(string header)
    {
        var m = HunkRx.Match(header);
        if (!m.Success)
            return (0, 0);
        return (
            int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    static Control BuildFileRow(FileVm vm)
    {
        var name = new TextBlock
        {
            Text = vm.ShortPath,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var badges = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 0, 0, 0),
            Children =
            {
                Badge($"+{vm.Added}", AddBadge),
                Badge($"−{vm.Deleted}", DelBadge),
            },
        };
        var row = new DockPanel { Margin = new Thickness(8, 5), LastChildFill = true };
        DockPanel.SetDock(badges, Dock.Right);
        row.Children.Add(badges);
        row.Children.Add(name);
        if (vm.IsBinary)
            name.Opacity = 0.7;
        return row;
    }

    static Control Badge(string text, IBrush fg) => new TextBlock
    {
        Text = text,
        FontFamily = Mono,
        FontSize = 11,
        FontWeight = FontWeight.SemiBold,
        Foreground = fg,
        VerticalAlignment = VerticalAlignment.Center,
    };

    static Control BuildLineRow(LineVm vm)
    {
        var (bg, fg) = vm.Kind switch
        {
            LineKind.Add => (AddBg, AddFg),
            LineKind.Del => (DelBg, DelFg),
            LineKind.Hunk => (HunkBg, HunkFg),
            LineKind.Meta => (PaneBg, Muted),
            _ => (ShellBg, TextBrush),
        };

        if (vm.Kind is LineKind.Hunk or LineKind.Meta)
        {
            return new Border
            {
                Background = bg,
                Padding = new Thickness(10, 4),
                Child = new TextBlock
                {
                    Text = vm.Text,
                    FontFamily = Mono,
                    FontSize = 12,
                    Foreground = fg,
                    TextWrapping = TextWrapping.NoWrap,
                },
            };
        }

        var oldG = Gutter(vm.OldNo);
        var newG = Gutter(vm.NewNo);
        var mark = new TextBlock
        {
            Text = vm.Kind switch
            {
                LineKind.Add => "+",
                LineKind.Del => "−",
                _ => " ",
            },
            Width = 14,
            FontFamily = Mono,
            FontSize = 12,
            Foreground = fg,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var body = new TextBlock
        {
            Text = vm.Text,
            FontFamily = Mono,
            FontSize = 12,
            Foreground = fg,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 10, 0),
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("48,48,14,*"),
            Background = bg,
            MinHeight = 20,
        };
        var gutterBand = new Border
        {
            Background = GutterBg,
            [Grid.ColumnSpanProperty] = 2,
        };
        Grid.SetColumn(oldG, 0);
        Grid.SetColumn(newG, 1);
        Grid.SetColumn(mark, 2);
        Grid.SetColumn(body, 3);
        grid.Children.Add(gutterBand);
        grid.Children.Add(oldG);
        grid.Children.Add(newG);
        grid.Children.Add(mark);
        grid.Children.Add(body);
        return grid;
    }

    static TextBlock Gutter(int? n) => new()
    {
        Text = n is null or 0 ? "" : n.Value.ToString(CultureInfo.InvariantCulture),
        FontFamily = Mono,
        FontSize = 11,
        Foreground = Muted,
        TextAlignment = TextAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 8, 0),
        Padding = new Thickness(0, 1),
    };

    enum LineKind { Context, Add, Del, Hunk, Meta }

    sealed class LineVm
    {
        public LineKind Kind { get; init; }
        public int? OldNo { get; init; }
        public int? NewNo { get; init; }
        public required string Text { get; init; }

        public static LineVm Context(int oldNo, int newNo, string text) => new()
        {
            Kind = LineKind.Context,
            OldNo = oldNo,
            NewNo = newNo,
            Text = text,
        };

        public static LineVm Add(int newNo, string text) => new()
        {
            Kind = LineKind.Add,
            NewNo = newNo,
            Text = text,
        };

        public static LineVm Del(int oldNo, string text) => new()
        {
            Kind = LineKind.Del,
            OldNo = oldNo,
            Text = text,
        };

        public static LineVm Hunk(string header) => new() { Kind = LineKind.Hunk, Text = header };
        public static LineVm Meta(string text) => new() { Kind = LineKind.Meta, Text = text };
    }

    sealed class FileVm
    {
        public required DiffFile Source { get; init; }
        public required string DisplayPath { get; init; }
        public required string ShortPath { get; init; }
        public required int Added { get; init; }
        public required int Deleted { get; init; }
        public required int HunkCount { get; init; }
        public bool IsBinary => Source.IsBinary;

        public static FileVm From(DiffFile file)
        {
            var added = 0;
            var deleted = 0;
            foreach (var h in file.Hunks)
            {
                foreach (var l in h.Lines)
                {
                    if (l.Kind == '+') added++;
                    else if (l.Kind == '-') deleted++;
                }
            }

            var display = file.OldPath is not null && !string.Equals(file.OldPath, file.Path, StringComparison.Ordinal)
                ? $"{file.OldPath} → {file.Path}"
                : file.Path;
            var shortPath = file.Path;
            if (shortPath.Length > 42)
                shortPath = "…" + shortPath[^41..];

            return new FileVm
            {
                Source = file,
                DisplayPath = display,
                ShortPath = shortPath,
                Added = added,
                Deleted = deleted,
                HunkCount = file.Hunks.Count,
            };
        }
    }
}
