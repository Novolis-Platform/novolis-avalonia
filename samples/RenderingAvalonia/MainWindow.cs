using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Novolis.Avalonia.Rendering;

namespace RenderingAvalonia;

internal sealed class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly CpuFrameDemo _cpuDemo;

    public MainWindow()
    {
        Title = "Novolis Rendering — Avalonia hosts";
        Width = 1280;
        Height = 720;

        var twoD = new TwoDSceneControl { MinHeight = 280 };
        _ = new PlatformerDemo(twoD);

        var cpu = new Rgba32FrameControl { MinHeight = 280 };
        _cpuDemo = new CpuFrameDemo(cpu, 320, 240);

        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            ],
            Margin = new Thickness(8),
        };
        Grid.SetColumn(twoD, 0);
        Grid.SetColumn(cpu, 1);
        grid.Children.Add(twoD);
        grid.Children.Add(cpu);

        Content = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new TextBlock
                {
                    Text = "Left: TwoDSceneControl (OpenGL)  |  Right: Rgba32FrameControl (CPU / path-trace presenter)",
                    Margin = new Thickness(8),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                grid,
            },
        };

        _timer.Tick += (_, _) => _cpuDemo.Tick(0.016f);
        _timer.Start();
    }
}
