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
