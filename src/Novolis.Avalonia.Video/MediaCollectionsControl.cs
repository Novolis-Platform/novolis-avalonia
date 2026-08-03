using Avalonia.Controls;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Collections pane listing <see cref="MediaAsset"/> rows for a <see cref="MovieProject"/>.</summary>
public sealed class MediaCollectionsControl : MovieEditPane
{
    readonly ListBox _list = new();

    /// <summary>Creates an empty collections pane.</summary>
    public MediaCollectionsControl()
    {
        Title = "Collections";
        Body = _list;
        _list.SelectionChanged += (_, _) => SelectionChanged?.Invoke();
    }

    /// <summary>Raised when the list selection changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>Gets the selected asset id, if any.</summary>
    public Guid? SelectedAssetId =>
        _list.SelectedItem is MediaAssetRow row ? row.Id : null;

    /// <summary>Rebuilds rows from <paramref name="project"/> assets.</summary>
    public void Bind(MovieProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var selected = SelectedAssetId;
        _list.ItemsSource = project.Assets
            .Select(a => new MediaAssetRow(a.Id, $"{a.Kind}: {a.Name} ({a.Duration.TotalSeconds:0.#}s)"))
            .ToList();

        if (selected is { } id)
        {
            foreach (var item in _list.Items)
            {
                if (item is MediaAssetRow row && row.Id == id)
                {
                    _list.SelectedItem = row;
                    break;
                }
            }
        }
    }

    /// <summary>One collections list row.</summary>
    public sealed record MediaAssetRow(Guid Id, string Label)
    {
        /// <inheritdoc />
        public override string ToString() => Label;
    }
}
