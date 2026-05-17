using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Layout;

/// <summary>Filter expression bar with apply and clear actions.</summary>
public sealed class FilterBar : Border
{
    private readonly TextBox _filterBox;

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

    public string FilterText => _filterBox.Text ?? string.Empty;

    public void SetFilterText(string text) => _filterBox.Text = text;

    public event EventHandler<string>? ApplyRequested;

    public event EventHandler? ClearRequested;
}
