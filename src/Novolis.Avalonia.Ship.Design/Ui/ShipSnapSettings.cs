using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>PLAN snapping / dimension readout controls.</summary>
public static class ShipSnapSettings
{
    public static Control Build(ShipDesignSession session)
    {
        var snap = new CheckBox { Content = "Snap", IsChecked = session.SnapEnabled };
        snap.IsCheckedChanged += (_, _) =>
        {
            session.SnapEnabled = snap.IsChecked == true;
            session.Notify();
        };

        var grid = new NumericUpDown
        {
            Value = (decimal)session.SnapGridMeters,
            Minimum = 0.05m,
            Maximum = 5m,
            Increment = 0.05m,
            FormatString = "0.##",
            Width = 72,
        };
        grid.ValueChanged += (_, _) =>
        {
            if (grid.Value is { } v)
            {
                session.SnapGridMeters = (float)v;
                session.Notify();
            }
        };

        var dims = new CheckBox { Content = "Dimensions", IsChecked = session.ShowDimensions };
        dims.IsCheckedChanged += (_, _) =>
        {
            session.ShowDimensions = dims.IsChecked == true;
            session.Notify();
        };

        var overlays = new CheckBox { Content = "Structure overlays", IsChecked = session.ShowStructuralOverlays };
        overlays.IsCheckedChanged += (_, _) =>
        {
            session.ShowStructuralOverlays = overlays.IsChecked == true;
            session.Notify();
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(8, 2),
        };
        row.Children.Add(snap);
        row.Children.Add(new TextBlock
        {
            Text = "Grid",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Gray,
        });
        row.Children.Add(grid);
        row.Children.Add(dims);
        row.Children.Add(overlays);
        return row;
    }
}
