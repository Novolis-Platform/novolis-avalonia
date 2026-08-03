using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Audio.Midi;

namespace Novolis.Avalonia.Audio;

/// <summary>Browse and select patches from an <see cref="InstrumentBank"/>.</summary>
public sealed class InstrumentBrowserControl : Border
{
    readonly ListBox _categories = new() { MinWidth = 110 };
    readonly ListBox _patches = new();
    InstrumentBank? _bank;
    string? _category;

    public InstrumentBrowserControl()
    {
        Background = AudioEditPalette.PaneAlt;
        BorderBrush = AudioEditPalette.Border;
        BorderThickness = new Thickness(1);
        Padding = new Thickness(8);
        CornerRadius = new CornerRadius(4);

        _categories.SelectionChanged += (_, _) =>
        {
            if (_categories.SelectedItem is string cat)
            {
                _category = cat;
                RefreshPatches();
            }
        };
        _patches.SelectionChanged += (_, _) =>
        {
            if (_patches.SelectedItem is PatchItem item)
                PatchSelected?.Invoke(item.Patch);
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("130,*"),
            ColumnSpacing = 8,
        };
        var catBorder = new Border
        {
            Background = AudioEditPalette.Pane,
            BorderBrush = AudioEditPalette.Border,
            BorderThickness = new Thickness(1),
            Child = _categories,
        };
        var patchBorder = new Border
        {
            Background = AudioEditPalette.Pane,
            BorderBrush = AudioEditPalette.Border,
            BorderThickness = new Thickness(1),
            Child = _patches,
        };
        Grid.SetColumn(catBorder, 0);
        Grid.SetColumn(patchBorder, 1);
        grid.Children.Add(catBorder);
        grid.Children.Add(patchBorder);

        Child = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new TextBlock
                {
                    Text = "Sounds",
                    FontSize = 16,
                    FontFamily = new FontFamily("Segoe UI Semibold"),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 8),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                grid,
            },
        };
    }

    /// <summary>Raised when the user picks a patch.</summary>
    public event Action<InstrumentPatch>? PatchSelected;

    /// <summary>Binds a bank and selects the first patch.</summary>
    public void Bind(InstrumentBank bank, string? selectedId = null)
    {
        _bank = bank ?? throw new ArgumentNullException(nameof(bank));
        var cats = bank.Categories.ToList();
        _categories.ItemsSource = cats;
        if (cats.Count > 0)
        {
            var preferCat = selectedId is not null
                ? bank.Find(selectedId)?.Category
                : null;
            _category = preferCat ?? cats[0];
            _categories.SelectedItem = _category;
        }

        RefreshPatches(selectedId);
    }

    void RefreshPatches(string? preferId = null)
    {
        if (_bank is null)
            return;
        var cat = _category ?? _bank.Categories.FirstOrDefault();
        var items = _bank.Patches
            .Where(p => string.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase))
            .Select(p => new PatchItem(p))
            .ToList();
        _patches.ItemsSource = items;
        if (items.Count == 0)
            return;

        var pick = preferId is null
            ? items[0]
            : items.FirstOrDefault(i => string.Equals(i.Patch.Id, preferId, StringComparison.OrdinalIgnoreCase))
              ?? items[0];
        _patches.SelectedItem = pick;
    }

    sealed record PatchItem(InstrumentPatch Patch)
    {
        public override string ToString() => Patch.Name;
    }
}
