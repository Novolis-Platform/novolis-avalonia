using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Object Manager | Viewport | Properties shell.</summary>
public sealed class SceneEditorSurface : UserControl
{
    private readonly SceneSessionService _session;
    private readonly ObjectManagerControl _objectManager;
    private readonly SceneViewportControl _viewport;
    private readonly PropertyInspectorControl _properties;
    private readonly DispatcherTimer _presentTimer;

    public SceneEditorSurface(SceneSessionService? session = null)
    {
        _session = session ?? new SceneSessionService();
        _objectManager = new ObjectManagerControl(_session) { Width = 260 };
        _viewport = new SceneViewportControl(_session);
        _properties = new PropertyInspectorControl(_session) { Width = 280 };
        var tools = new SceneToolStrip(_session);

        var chrome = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 32, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 60, 75)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = tools,
            [DockPanel.DockProperty] = Dock.Top,
        };

        var split = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("260,* ,280"),
        };
        split.Children.Add(_objectManager);
        Grid.SetColumn(_objectManager, 0);
        split.Children.Add(_viewport);
        Grid.SetColumn(_viewport, 1);
        split.Children.Add(_properties);
        Grid.SetColumn(_properties, 2);

        Content = new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(14, 20, 28)),
            Children = { chrome, split },
        };

        _session.DocumentChanged += () =>
        {
            _objectManager.Refresh();
            _properties.Refresh();
        };

        _presentTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) =>
        {
            _viewport.Host.RequestFrame();
            _viewport.InvalidateVisual();
        });

        AttachedToVisualTree += (_, _) =>
        {
            _viewport.Start();
            _presentTimer.Start();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _presentTimer.Stop();
            _viewport.Stop();
        };
    }

    public SceneSessionService Session => _session;
    public SceneViewportControl Viewport => _viewport;

    public void Fit() => _viewport.Fit();
}
