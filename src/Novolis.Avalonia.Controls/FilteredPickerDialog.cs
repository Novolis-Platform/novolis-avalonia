using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Novolis.Avalonia.Controls;

/// <summary>Modal filtered list picker (go-to / reference picker patterns).</summary>
public sealed class FilteredPickerDialog<T> : Window
    where T : class
{
    readonly TaskCompletionSource<T?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ListBox _list;
    readonly Func<T, string> _display;
    readonly Func<T, string, bool> _filter;
    readonly IReadOnlyList<T> _all;
    IReadOnlyList<T> _visible;

    FilteredPickerDialog(
        string title,
        IReadOnlyList<T> items,
        Func<T, string> display,
        Func<T, string, bool>? filter)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(display);

        Title = title;
        Width = 500;
        Height = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        _all = items;
        _display = display;
        _filter = filter ?? FilteredPickerLogic.ContainsDisplay(display);
        _visible = _all;

        var search = new TextBox { PlaceholderText = "Filter..." };
        search.TextChanged += (_, _) => ApplyFilter(search.Text);

        _list = new ListBox();
        _list.DoubleTapped += OnConfirm;
        _list.KeyDown += OnListKeyDown;
        RefreshList();

        var cancel = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Right };
        cancel.Click += (_, _) => Complete(null);

        var root = new Grid
        {
            Margin = new Thickness(12),
            RowDefinitions = new RowDefinitions("Auto,*,Auto")
        };
        Grid.SetRow(search, 0);
        Grid.SetRow(_list, 1);
        Grid.SetRow(cancel, 2);
        search.Margin = new Thickness(0, 0, 0, 8);
        cancel.Margin = new Thickness(0, 8, 0, 0);
        root.Children.Add(search);
        root.Children.Add(_list);
        root.Children.Add(cancel);
        Content = root;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Complete(null);
                e.Handled = true;
            }
        };

        Closed += (_, _) => _tcs.TrySetResult(null);
        Opened += (_, _) => search.Focus();
    }

    void ApplyFilter(string? query)
    {
        _visible = FilteredPickerLogic.Filter(_all, query, _filter);
        RefreshList();
    }

    void RefreshList()
    {
        _list.Items.Clear();
        foreach (var item in _visible)
            _list.Items.Add(new ListBoxItem { Content = _display(item), Tag = item });
        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmSelection();
            e.Handled = true;
        }
    }

    void OnConfirm(object? sender, RoutedEventArgs e) => ConfirmSelection();

    void ConfirmSelection()
    {
        if (_list.SelectedItem is ListBoxItem { Tag: T item })
            Complete(item);
    }

    void Complete(T? value)
    {
        if (_tcs.TrySetResult(value))
            Close();
    }

    /// <summary>Shows a filtered picker and returns the selected item, or null if cancelled.</summary>
    public static async Task<T?> ShowAsync(
        Window? owner,
        string title,
        IReadOnlyList<T> items,
        Func<T, string> display,
        Func<T, string, bool>? filter = null)
    {
        var dialog = new FilteredPickerDialog<T>(title, items, display, filter);
        if (owner is not null)
            await dialog.ShowDialog(owner).ConfigureAwait(true);
        else
            dialog.Show();
        return await dialog._tcs.Task.ConfigureAwait(true);
    }
}
