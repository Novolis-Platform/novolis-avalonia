using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>PLAN snapping / ortho / angle lock controls.</summary>
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

        var ortho = new CheckBox { Content = "Ortho (F8)", IsChecked = session.OrthoLocked };
        ortho.IsCheckedChanged += (_, _) => session.SetOrthoLocked(ortho.IsChecked == true);

        var ang15 = new CheckBox { Content = "Ang15 (F7)", IsChecked = session.AngleLockEnabled };
        ang15.IsCheckedChanged += (_, _) => session.SetAngleLockEnabled(ang15.IsChecked == true);

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

        session.Changed += () =>
        {
            ortho.IsChecked = session.OrthoLocked;
            ang15.IsChecked = session.AngleLockEnabled;
            snap.IsChecked = session.SnapEnabled;
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
        row.Children.Add(ortho);
        row.Children.Add(ang15);
        row.Children.Add(dims);
        row.Children.Add(overlays);
        row.Children.Add(new TextBlock
        {
            Text = "Shift=ortho · Ctrl=15° · Alt=free · MMB=pan",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.DimGray,
            FontSize = 11,
            Margin = new Thickness(8, 0, 0, 0),
        });
        return row;
    }
}
