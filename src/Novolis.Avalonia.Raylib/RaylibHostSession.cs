using Novolis.Math.Geometry;
using Novolis.Raylib.Abstractions;
using Novolis.Raylib.Shell;

namespace Novolis.Avalonia.Raylib;

/// <summary>Background Raylib loop for <see cref="RaylibHostControl"/>.</summary>
internal sealed class RaylibHostSession : IDisposable
{
    private readonly IRaylibFrameRenderer _renderer;
    private readonly RaylibEmbeddedOptions _options;
    private readonly object _frameLock = new();
    private Thread? _thread;
    private CancellationTokenSource? _cts;
    private Rgba32[]? _latestPixels;
    private int _latestWidth;
    private int _latestHeight;
    private bool _hasFrame;

    public RaylibHostSession(RaylibEmbeddedOptions options, IRaylibFrameRenderer renderer)
    {
        _options = options;
        _renderer = renderer;
    }

    public bool IsRunning => _thread is { IsAlive: true };

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _thread = new Thread(() => RunLoop(_cts.Token))
        {
            IsBackground = true,
            Name = "Novolis.Raylib.AvaloniaHost",
        };
        _thread.Start();
    }

    public bool TryTakeFrame(out Rgba32[] pixels, out int width, out int height)
    {
        lock (_frameLock)
        {
            if (!_hasFrame || _latestPixels is null)
            {
                pixels = [];
                width = 0;
                height = 0;
                return false;
            }

            pixels = _latestPixels;
            width = _latestWidth;
            height = _latestHeight;
            _hasFrame = false;
            return true;
        }
    }

    public void Dispose()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
        }

        _thread?.Join(TimeSpan.FromSeconds(5));
        _cts?.Dispose();
    }

    private void RunLoop(CancellationToken cancellationToken)
    {
        try
        {
            RaylibEmbeddedShell.Run(_options, _renderer, OnEmbeddedFrame, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
    }

    private void OnEmbeddedFrame(RaylibEmbeddedFrame frame)
    {
        var rgba = frame.RgbaPixels.Span;
        var pixels = new Rgba32[frame.Width * frame.Height];
        for (var i = 0; i < pixels.Length; i++)
        {
            var o = i * 4;
            pixels[i] = new Rgba32(rgba[o], rgba[o + 1], rgba[o + 2], rgba[o + 3]);
        }

        lock (_frameLock)
        {
            _latestPixels = pixels;
            _latestWidth = frame.Width;
            _latestHeight = frame.Height;
            _hasFrame = true;
        }
    }
}
