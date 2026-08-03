using Avalonia.Threading;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Wires <see cref="EditTransport"/> + <see cref="MoviePreviewComposer"/> to a <see cref="VideoSurface"/>.</summary>
public sealed class MoviePreviewSession : IDisposable
{
    readonly MovieProject _project;
    readonly EditTransport _transport;
    readonly MoviePreviewComposer _composer;
    readonly VideoSurface _surface;
    readonly StoryboardStrip? _storyboard;
    readonly DispatcherTimer _timer;
    bool _disposed;

    /// <summary>Creates a preview loop for the given project and surface.</summary>
    public MoviePreviewSession(
        MovieProject project,
        EditTransport transport,
        MoviePreviewComposer composer,
        VideoSurface surface,
        StoryboardStrip? storyboard = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _storyboard = storyboard;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += OnTick;
        _transport.Changed += OnTransportChanged;
        Present();
    }

    /// <summary>Gets the bound transport.</summary>
    public EditTransport Transport => _transport;

    /// <summary>Starts the ~30 FPS preview timer.</summary>
    public void Start() => _timer.Start();

    /// <summary>Stops the preview timer.</summary>
    public void Stop() => _timer.Stop();

    /// <summary>Forces a frame present at the current playhead.</summary>
    public void Refresh() => Present();

    void OnTick(object? sender, EventArgs e)
    {
        var duration = StoryboardQuery.TotalDuration(_project);
        if (_transport.Tick(TimeSpan.FromMilliseconds(33), duration))
            Present();
    }

    void OnTransportChanged() => Present();

    void Present()
    {
        var frame = _composer.Compose(_project, _transport.Position);
        _surface.Present(frame);
        _storyboard?.SetPlayhead(_transport.Position);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _transport.Changed -= OnTransportChanged;
    }
}
