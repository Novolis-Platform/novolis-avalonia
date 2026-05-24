using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Layout;

/// <summary>Filter expression bar with apply and clear actions.</summary>
public sealed class FilterBar : Border
{
    private readonly TextBox _filterBox;

    /// <summary>Creates a filter bar with the given label prefix.</summary>
    /// <param name="labelText">Text shown before the filter text box.</param>
    public FilterBar(string labelText = "Filter:")
    {
        Padding = new Thickness(8, 4);
        _filterBox = new TextBox { PlaceholderText = "BPF expression (applied on next capture start)" };

        var apply = new Button { Content = "Apply" };
        apply.Click += (_, _) => ApplyRequested?.Invoke(this, FilterText);

        var clear = new Button { Content = "Clear" };
        clear.Click += (_, _) =>
        {
            _filterBox.Text = string.Empty;
            ClearRequested?.Invoke(this, EventArgs.Empty);
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        row.Children.Add(new TextBlock
        {
            Text = labelText,
            VerticalAlignment = VerticalAlignment.Center,
        });
        row.Children.Add(_filterBox);
        row.Children.Add(apply);
        row.Children.Add(clear);
        Child = row;
    }

    /// <summary>Current filter expression text.</summary>
    public string FilterText => _filterBox.Text ?? string.Empty;

    /// <summary>Sets the filter text box contents.</summary>
    /// <param name="text">Expression to display.</param>
    public void SetFilterText(string text) => _filterBox.Text = text;

    /// <summary>Raised when the user clicks Apply.</summary>
    public event EventHandler<string>? ApplyRequested;

    /// <summary>Raised when the user clicks Clear.</summary>
    public event EventHandler? ClearRequested;
}
