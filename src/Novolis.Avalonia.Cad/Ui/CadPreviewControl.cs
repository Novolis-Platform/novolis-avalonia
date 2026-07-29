using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Novolis.Avalonia.Raylib;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>
/// Preview viewport owning <see cref="RaylibHostControl"/> lifecycle + present loop.
/// Consumers subscribe to <see cref="FrameRendering"/> for scene draw.
/// </summary>
public sealed class CadPreviewControl : Panel
{
    private readonly DispatcherTimer _timer;
    private bool _started;

    public CadPreviewControl()
    {
        Host = new RaylibHostControl();
        Children.Add(Host);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) =>
        {
            if (!_started)
                return;
            Host.RequestFrame();
            Host.InvalidateVisual();
        };

        Host.PointerPressed += OnPointerPressed;
        Host.PointerMoved += OnPointerMoved;
        Host.PointerReleased += OnPointerReleased;
        Host.PointerWheelChanged += OnWheel;
    }

    public RaylibHostControl Host { get; }

    public event EventHandler<RaylibFrameEventArgs>? FrameRendering
    {
        add => Host.FrameRendering += value;
        remove => Host.FrameRendering -= value;
    }

    public event Action<float, float>? OrbitDrag;

    public event Action<float>? Zoom;

    public void Start()
    {
        Host.SetHostActive(true);
        Host.EnsureHostStarted();
        _started = true;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _started = false;
        Host.SetHostActive(false);
    }

    private Point? _last;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(Host).Properties.IsLeftButtonPressed)
            _last = e.GetPosition(Host);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_last is null || !e.GetCurrentPoint(Host).Properties.IsLeftButtonPressed)
            return;
        var p = e.GetPosition(Host);
        var dx = (float)(p.X - _last.Value.X);
        var dy = (float)(p.Y - _last.Value.Y);
        _last = p;
        OrbitDrag?.Invoke(dx, dy);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => _last = null;

    private void OnWheel(object? sender, PointerWheelEventArgs e) =>
        Zoom?.Invoke((float)e.Delta.Y);
}
