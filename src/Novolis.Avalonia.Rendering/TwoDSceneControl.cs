using System.Diagnostics;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Novolis.Rendering.Backends.TwoD.Silk;
using Novolis.Rendering.TwoD;

namespace Novolis.Avalonia.Rendering;

/// <summary>
/// Avalonia <see cref="OpenGlControlBase"/> that draws a <see cref="TwoDScene"/> via <see cref="SilkTwoDRenderer"/>.
/// </summary>
public class TwoDSceneControl : OpenGlControlBase
{
    /// <summary>Scene to draw (textures, sprites, HUD, menus).</summary>
    public static readonly StyledProperty<TwoDScene?> SceneProperty =
        AvaloniaProperty.Register<TwoDSceneControl, TwoDScene?>(nameof(Scene));

    private SilkTwoDRenderer? _renderer;
    private Stopwatch? _clock;
    private double _lastSeconds;

    /// <summary>Scene to draw.</summary>
    public TwoDScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    /// <summary>Raised before each draw with elapsed seconds since the previous frame.</summary>
    public event EventHandler<TwoDFrameEventArgs>? FrameUpdating;

    /// <summary>Starts the render loop when attached to the visual tree.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _clock = Stopwatch.StartNew();
        _lastSeconds = 0d;
        RequestNextFrameRendering();
    }

    /// <inheritdoc />
    protected override void OnOpenGlInit(GlInterface gl)
    {
        _renderer = new SilkTwoDRenderer(SilkGlBridge.CreateGl(gl));
    }

    /// <inheritdoc />
    protected override void OnOpenGlRender(GlInterface gl, int framebuffer)
    {
        var scene = Scene;
        if (scene is null || _renderer is null)
        {
            return;
        }

        var bounds = Bounds;
        var width = System.Math.Max(1, (int)bounds.Width);
        var height = System.Math.Max(1, (int)bounds.Height);
        _renderer.Resize(width, height);

        var now = _clock?.Elapsed.TotalSeconds ?? 0d;
        var delta = (float)System.Math.Max(0d, now - _lastSeconds);
        _lastSeconds = now;
        if (delta > 0f)
        {
            FrameUpdating?.Invoke(this, new TwoDFrameEventArgs(delta));
            scene.Update(delta);
        }

        _renderer.DrawScene(scene);
        RequestNextFrameRendering();
    }

    /// <inheritdoc />
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _renderer?.Dispose();
        _renderer = null;
    }
}

/// <summary>Per-frame update args for <see cref="TwoDSceneControl"/>.</summary>
public sealed class TwoDFrameEventArgs(float deltaSeconds) : EventArgs
{
    /// <summary>Elapsed time since the previous frame in seconds.</summary>
    public float DeltaSeconds { get; } = deltaSeconds;
}
