using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Scene hierarchy | Viewport | Properties shell. Chrome is created for hosts to dock.</summary>
public sealed class SceneEditorSurface : UserControl
{
    private readonly SceneSessionService _session;
    private readonly DispatcherTimer _presentTimer;

    public SceneEditorSurface(
        SceneSessionService? session = null,
        bool composeDefaultLayout = true,
        SceneViewportBackendKind backend = SceneViewportBackendKind.OpenGl)
    {
        _session = session ?? new SceneSessionService();
        ObjectManager = new ObjectManagerControl(_session) { Width = 260 };
        Viewport = new SceneViewportControl(_session, backend);
        Properties = new PropertyInspectorControl(_session) { Width = 280 };
        EditModeBar = new SceneEditModeBar(_session);
        DisplayModeBar = new SceneDisplayModeBar(_session);
        PrimitivePalette = new PrimitivePalette(_session);
        GeneratorTools = new GeneratorToolStrip(_session);
        MeshEditTools = new MeshEditToolStrip(_session);
        LookTools = new LookToolStrip(_session);
        MeshAttributes = new MeshAttributePanel(_session) { Width = 280 };
        TransformHud = new TransformHud(_session);
        ModifierStack = new ModifierStackPanel(_session) { Width = 280 };
        StatusBar = new ViewportStatusBar(_session);
        ToolStrip = new SceneToolStrip(_session, onFit: () => Viewport.Fit());

        _session.DocumentChanged += () =>
        {
            ObjectManager.Refresh();
            Properties.Refresh();
            MeshAttributes.Refresh();
            ModifierStack.Refresh();
            StatusBar.Refresh(_session);
            Viewport.RequestPresent();
        };

        Viewport.Camera.Changed += () => Viewport.RequestPresent();
        _session.FitRequested += () =>
        {
            if (Dispatcher.UIThread.CheckAccess())
                Viewport.Fit();
            else
                Dispatcher.UIThread.Post(() => Viewport.Fit());
        };

        _presentTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Background, (_, _) =>
        {
            // Only keep presenting while orbiting — free-run 60 Hz clears flicker on large meshes.
            if (Viewport.Camera.CameraInteracting)
                Viewport.RequestPresent();
        });

        AttachedToVisualTree += (_, _) =>
        {
            Viewport.Start();
            _presentTimer.Start();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _presentTimer.Stop();
            Viewport.Stop();
        };

        if (composeDefaultLayout)
            Content = BuildDefaultLayout();
    }

    public SceneSessionService Session => _session;
    public ObjectManagerControl ObjectManager { get; }
    public SceneViewportControl Viewport { get; }
    public PropertyInspectorControl Properties { get; }
    public SceneEditModeBar EditModeBar { get; }
    public SceneDisplayModeBar DisplayModeBar { get; }
    public PrimitivePalette PrimitivePalette { get; }
    public GeneratorToolStrip GeneratorTools { get; }
    public MeshEditToolStrip MeshEditTools { get; }
    public LookToolStrip LookTools { get; }
    public MeshAttributePanel MeshAttributes { get; }
    public TransformHud TransformHud { get; }
    public ModifierStackPanel ModifierStack { get; }
    public ViewportStatusBar StatusBar { get; }
    public SceneToolStrip ToolStrip { get; }

    /// <summary>Builds the shared two-row chrome (call once per surface — reparents tool strips).</summary>
    public SceneChromeShell CreateChrome(string? dumpsDirectoryTooltip = null) =>
        new(this, dumpsDirectoryTooltip);

    public void Fit() => Viewport.Fit();

    /// <summary>Hosts that dock chrome themselves must call this (AttachedToVisualTree will not fire on an unused surface).</summary>
    public void StartPresenting()
    {
        Viewport.Start();
        if (!_presentTimer.IsEnabled)
            _presentTimer.Start();
    }

    public void StopPresenting()
    {
        _presentTimer.Stop();
        Viewport.Stop();
    }

    public Control BuildDefaultLayout()
    {
        var right = new DockPanel
        {
            Width = 300,
            Children =
            {
                new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        Children = { MeshAttributes, ModifierStack, Properties },
                    },
                },
            },
        };

        var split = new Grid { ColumnDefinitions = new ColumnDefinitions("260,*,300") };
        Grid.SetColumn(ObjectManager, 0);
        Grid.SetColumn(Viewport, 1);
        Grid.SetColumn(right, 2);
        split.Children.Add(ObjectManager);
        split.Children.Add(Viewport);
        split.Children.Add(right);

        var chrome = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 32, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 60, 75)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = CreateChrome(),
            [DockPanel.DockProperty] = Dock.Top,
        };

        var status = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 32, 42)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 60, 75)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = StatusBar,
            [DockPanel.DockProperty] = Dock.Bottom,
        };

        return new DockPanel
        {
            Background = new SolidColorBrush(Color.FromRgb(14, 20, 28)),
            Children = { chrome, status, split },
        };
    }
}
