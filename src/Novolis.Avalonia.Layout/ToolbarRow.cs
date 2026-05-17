using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Layout;

/// <summary>Horizontal toolbar with leading actions, stretch, and trailing status text.</summary>
public sealed class ToolbarRow : Border
{
    private readonly StackPanel _actions;
    private readonly TextBlock _status;

    public ToolbarRow()
    {
        Padding = new Thickness(8, 6);
        _actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };
        _status = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
        };

        var row = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            ],
        };
        row.Children.Add(_actions);
        Grid.SetColumn(_actions, 0);
        row.Children.Add(_status);
        Grid.SetColumn(_status, 1);
        Child = row;
    }

    public void AddAction(Control control) => _actions.Children.Add(control);

    public string StatusText
    {
        get => _status.Text ?? string.Empty;
        set => _status.Text = value;
    }
}
