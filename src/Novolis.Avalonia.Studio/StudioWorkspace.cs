using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Novolis.Avalonia.Studio;

/// <summary>Three-column editor shell: left rail, center (toolbar + body), right rail.</summary>
public sealed class StudioWorkspace : Grid
{
    public StudioWorkspace(
        Control leftRail,
        Control centerBody,
        Control rightRail,
        double leftWidth = 300,
        double rightWidth = 320)
    {
        ColumnDefinitions = new ColumnDefinitions($"{leftWidth},*,{rightWidth}");
        Grid.SetColumn(leftRail, 0);
        Children.Add(leftRail);
        Grid.SetColumn(centerBody, 1);
        Children.Add(centerBody);
        Grid.SetColumn(rightRail, 2);
        Children.Add(rightRail);
    }

    public static Grid CreateCenterColumn(Control toolbar, Control body)
    {
        var center = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        center.Children.Add(toolbar);
        Grid.SetRow(body, 1);
        center.Children.Add(body);
        return center;
    }

    public static DockPanel CreateViewportStack(Control viewportHost, TextBlock flashLine, TextBlock statusLine)
    {
        var panel = new DockPanel();
        DockPanel.SetDock(flashLine, Dock.Bottom);
        DockPanel.SetDock(statusLine, Dock.Bottom);
        panel.Children.Add(flashLine);
        panel.Children.Add(statusLine);
        panel.Children.Add(viewportHost);
        return panel;
    }

    public static StackPanel CreateToolbarRow() =>
        new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8),
        };

    public static Border ToolbarSeparator() => new() { Width = 8 };
}
