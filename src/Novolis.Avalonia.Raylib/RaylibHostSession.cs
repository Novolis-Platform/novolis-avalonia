using System.Reflection;
using System.Threading.Channels;
using Novolis.Math.Geometry;
using Novolis.Raylib.Abstractions;
using Novolis.Raylib.Shell;

namespace Novolis.Avalonia.Raylib;

/// <summary>Background Raylib host for <see cref="RaylibHostControl"/> (on-demand when available, else streaming loop).</summary>
internal sealed class RaylibHostSession : IDisposable
{
    private static readonly TimeSpan IdleRefreshInterval = TimeSpan.FromMilliseconds(250);
    private static readonly Type? OnDemandHostType =
        typeof(RaylibEmbeddedShell).Assembly.GetType("Novolis.Raylib.Shell.RaylibEmbeddedHost", throwOnError: false);

    private readonly IRaylibFrameRenderer _renderer;
    private readonly RaylibEmbeddedOptions _options;
    private readonly Channel<HostRenderRequest> _requestChannel;
    private readonly Channel<HostFramePacket> _frameChannel;
    private readonly string _windowTitle;
    private readonly bool _hideWindow;
    private readonly bool _disableExitKey;
    private int _width;
    private int _height;
    private int _targetFps;
    private Thread? _thread;
    private CancellationTokenSource? _cts;
    private DateTimeOffset _lastFrameAt = DateTimeOffset.MinValue;
    private object? _onDemandHost;

    public RaylibHostSession(RaylibEmbeddedOptions options, IRaylibFrameRenderer renderer)
    {
        _renderer = renderer;
        _width = System.Math.Clamp(options.Width, 64, 4096);
        _height = System.Math.Clamp(options.Height, 64, 4096);
        _targetFps = System.Math.Clamp(options.TargetFps, 1, 240);
        _windowTitle = options.WindowTitle;
        _hideWindow = options.HideWindow;
        _disableExitKey = options.DisableExitKey;
        _options = options;
        _requestChannel = Channel.CreateBounded<HostRenderRequest>(new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
        _frameChannel = Channel.CreateBounded<HostFramePacket>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = true,
        });
    }

    public bool IsRunning => _thread is { IsAlive: true };

    public DateTimeOffset LastFrameAt => _lastFrameAt;

    public void Start()
    {
        if (IsRunning)
            return;

        _cts = new CancellationTokenSource();
        _thread = new Thread(() => RunLoop(_cts.Token))
        {
            IsBackground = true,
            Name = "Novolis.Raylib.AvaloniaHost",
        };
        _thread.Start();
        RequestRedraw();
    }

    public void RequestRedraw() =>
        _requestChannel.Writer.TryWrite(new HostRenderRequest(HostRenderRequestKind.Redraw));

    public void RequestResize(int width, int height, int targetFps)
    {
        _width = System.Math.Clamp(width, 64, 4096);
        _height = System.Math.Clamp(height, 64, 4096);
        _targetFps = System.Math.Clamp(targetFps, 1, 240);
        _requestChannel.Writer.TryWrite(new HostRenderRequest(HostRenderRequestKind.Resize));
        _requestChannel.Writer.TryWrite(new HostRenderRequest(HostRenderRequestKind.Redraw));
    }

    public bool TryReadFrame(out HostFramePacket? packet)
    {
        if (_frameChannel.Reader.TryRead(out var frame))
        {
            packet = frame;
            return true;
        }

        packet = null;
        return false;
    }

    public void Dispose()
    {
        _requestChannel.Writer.TryWrite(new HostRenderRequest(HostRenderRequestKind.Shutdown));
        if (_cts is not null)
            _cts.Cancel();

        _thread?.Join(TimeSpan.FromSeconds(5));
        _cts?.Dispose();
        DisposeOnDemandHost();
    }

    private void RunLoop(CancellationToken cancellationToken)
    {
        try
        {
            if (OnDemandHostType is not null && TryRunOnDemandLoop(cancellationToken))
                return;

            RunLegacyStreamLoop(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
    }

    private bool TryRunOnDemandLoop(CancellationToken cancellationToken)
    {
        var create = OnDemandHostType!.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        if (create is null)
            return false;

        _onDemandHost = create.Invoke(null, [CloneOptions()]);
        if (_onDemandHost is null)
            return false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var work = CollectWork(cancellationToken);
                if (work == HostRenderRequestKind.Shutdown)
                    break;

                if (work == HostRenderRequestKind.Resize)
                    OnDemandHostType.GetMethod("Resize")?.Invoke(_onDemandHost, [CloneOptions()]);

                OnDemandHostType.GetMethod("TryRenderOneFrame")?.Invoke(
                    _onDemandHost,
                    [_renderer, (Action<RaylibEmbeddedFrame>)OnEmbeddedFrame]);
            }

            return true;
        }
        finally
        {
            DisposeOnDemandHost();
        }
    }

    private void DisposeOnDemandHost()
    {
        if (_onDemandHost is null)
            return;

        OnDemandHostType?.GetMethod("Dispose")?.Invoke(_onDemandHost, null);
        _onDemandHost = null;
    }

    private void RunLegacyStreamLoop(CancellationToken cancellationToken)
    {
        var options = CloneOptions();
        RaylibEmbeddedShell.Run(options, _renderer, OnEmbeddedFrame, cancellationToken);
    }

    private HostRenderRequestKind CollectWork(CancellationToken cancellationToken)
    {
        var resize = false;
        while (_requestChannel.Reader.TryRead(out var pending))
        {
            if (pending.Kind == HostRenderRequestKind.Shutdown)
                return HostRenderRequestKind.Shutdown;
            if (pending.Kind == HostRenderRequestKind.Resize)
                resize = true;
        }

        if (resize)
            return HostRenderRequestKind.Resize;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(IdleRefreshInterval);
        try
        {
            _requestChannel.Reader.WaitToReadAsync(timeoutCts.Token).AsTask().GetAwaiter().GetResult();
            while (_requestChannel.Reader.TryRead(out var pending))
            {
                if (pending.Kind == HostRenderRequestKind.Shutdown)
                    return HostRenderRequestKind.Shutdown;
                if (pending.Kind == HostRenderRequestKind.Resize)
                    resize = true;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Idle refresh.
        }

        return resize ? HostRenderRequestKind.Resize : HostRenderRequestKind.Redraw;
    }

    private RaylibEmbeddedOptions CloneOptions() =>
        new()
        {
            Width = _width,
            Height = _height,
            WindowTitle = _windowTitle,
            TargetFps = _targetFps,
            HideWindow = _hideWindow,
            DisableExitKey = _disableExitKey,
        };

    private void OnEmbeddedFrame(RaylibEmbeddedFrame frame)
    {
        var rgba = frame.RgbaPixels.Span;
        var pixels = new Rgba32[frame.Width * frame.Height];
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            pixels[i] = new Rgba32(rgba[o], rgba[o + 1], rgba[o + 2], rgba[o + 3]);
        }

        _lastFrameAt = DateTimeOffset.UtcNow;
        _frameChannel.Writer.TryWrite(new HostFramePacket(pixels, frame.Width, frame.Height, _lastFrameAt));
    }
}
