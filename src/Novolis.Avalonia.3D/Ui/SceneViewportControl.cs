using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Raylib;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;

namespace Novolis.Avalonia._3D.Ui;

public sealed class SceneViewportControl : Panel
{
    private readonly RaylibHostControl _host = new();
    private readonly SceneViewportRenderer _renderer;
    private Point? _last;
    private bool _orbiting;

    public SceneViewportControl(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _renderer = new SceneViewportRenderer(session);
        Background = new SolidColorBrush(Color.FromRgb(18, 24, 32));
        Children.Add(_host);
        _renderer.Bind(_host);

        PointerPressed += OnPressed;
        PointerMoved += OnMoved;
        PointerReleased += (_, _) => { _orbiting = false; _last = null; };
        PointerWheelChanged += (_, e) =>
        {
            _renderer.Zoom((float)e.Delta.Y);
            e.Handled = true;
        };
    }

    public RaylibHostControl Host => _host;
    public SceneViewportRenderer Renderer => _renderer;

    public void Start()
    {
        _host.SetHostActive(true);
        _host.EnsureHostStarted();
    }

    public void Stop() => _host.SetHostActive(false);

    public void Fit() => _renderer.Fit();

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        _orbiting = true;
        _last = e.GetPosition(this);
        e.Pointer.Capture(this);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_orbiting || _last is null)
            return;
        var p = e.GetPosition(this);
        var dx = (float)(p.X - _last.Value.X);
        var dy = (float)(p.Y - _last.Value.Y);
        _last = p;
        _renderer.OrbitDrag(dx, dy);
    }
}
