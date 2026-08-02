using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Novolis.Rendering.Backends.TwoD.Silk;
using Novolis.Rendering.TwoD;
using Silk.NET.OpenGL;
using PresentationMouseButton = Novolis.Rendering.Presentation.MouseButton;
using AvaloniaMouseButton = global::Avalonia.Input.MouseButton;

namespace Novolis.Avalonia.Rendering;

/// <summary>
/// Avalonia <see cref="OpenGlControlBase"/> that draws a <see cref="TwoDScene"/> via <see cref="SilkTwoDRenderer"/>.
/// Exposes DPI-correct framebuffer mouse input for map/game hit-testing.
/// </summary>
/// <remarks>
/// Implements <see cref="ICustomHitTest"/> — OpenGlControlBase has no background, so without this
/// Avalonia never delivers pointer events to the GL surface.
/// </remarks>
public class TwoDSceneControl : OpenGlControlBase, ICustomHitTest
{
    /// <summary>Scene to draw (textures, sprites, HUD, menus).</summary>
    public static readonly StyledProperty<TwoDScene?> SceneProperty =
        AvaloniaProperty.Register<TwoDSceneControl, TwoDScene?>(nameof(Scene));

    private SilkTwoDRenderer? _renderer;
    private GL? _gl;
    private Stopwatch? _clock;
    private double _lastSeconds;
    private int _pixelWidth = 1;
    private int _pixelHeight = 1;
    private int _captureTick;
    private readonly object _captureGate = new();
    private byte[]? _lastPng;

    private Vector2 _mousePixel;
    private Vector2 _mousePixelLastFrame;
    private readonly HashSet<PresentationMouseButton> _mouseDown = new();
    private readonly HashSet<PresentationMouseButton> _mousePressedThisFrame = new();
    private readonly HashSet<PresentationMouseButton> _mouseReleasedThisFrame = new();

    /// <summary>Creates a focusable GL viewport that accepts pointer input.</summary>
    public TwoDSceneControl()
    {
        Focusable = true;
        IsHitTestVisible = true;
        // Required with ICustomHitTest so hits stay inside control bounds.
        ClipToBounds = true;
    }

    /// <inheritdoc />
    public bool HitTest(Point point) => true;

    /// <summary>Scene to draw.</summary>
    public TwoDScene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    /// <summary>Framebuffer width in pixels (DPI-scaled).</summary>
    public int PixelWidth => _pixelWidth;

    /// <summary>Framebuffer height in pixels (DPI-scaled).</summary>
    public int PixelHeight => _pixelHeight;

    /// <summary>Cursor position in framebuffer pixels (origin top-left).</summary>
    public Vector2 MousePixelPosition => _mousePixel;

    /// <summary>Cursor delta since the previous <see cref="FrameUpdating"/> in pixels.</summary>
    public Vector2 MousePixelDelta => _mousePixel - _mousePixelLastFrame;

    /// <summary>Raised before each draw with elapsed seconds since the previous frame.</summary>
    public event EventHandler<TwoDFrameEventArgs>? FrameUpdating;

    /// <summary>Pointer pressed in framebuffer pixel space (after DPI scale).</summary>
    public event EventHandler<TwoDPointerEventArgs>? ScenePointerPressed;

    /// <summary>Pointer released in framebuffer pixel space.</summary>
    public event EventHandler<TwoDPointerEventArgs>? ScenePointerReleased;

    /// <summary>Pointer moved in framebuffer pixel space.</summary>
    public event EventHandler<TwoDPointerEventArgs>? ScenePointerMoved;

    /// <summary>
    /// Latest PNG of the GL framebuffer (for agent screenshots — <c>RenderTargetBitmap</c> cannot see OpenGL).
    /// </summary>
    public byte[]? TryGetLastFramePng()
    {
        lock (_captureGate)
            return _lastPng is null ? null : (byte[])_lastPng.Clone();
    }

    /// <summary>Whether <paramref name="button"/> is currently held.</summary>
    public bool IsMouseButtonDown(PresentationMouseButton button) => _mouseDown.Contains(button);

    /// <summary>True if <paramref name="button"/> transitioned to down since the previous frame.</summary>
    public bool IsMouseButtonPressed(PresentationMouseButton button) => _mousePressedThisFrame.Contains(button);

    /// <summary>True if <paramref name="button"/> transitioned to up since the previous frame.</summary>
    public bool IsMouseButtonReleased(PresentationMouseButton button) => _mouseReleasedThisFrame.Contains(button);

    /// <summary>Converts a control-local Avalonia point to framebuffer pixels.</summary>
    public Vector2 ToPixelPosition(Point controlLocal)
    {
        RefreshPixelSize();
        var scale = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
        return new Vector2((float)(controlLocal.X * scale), (float)(controlLocal.Y * scale));
    }

    /// <summary>
    /// Injects a press in framebuffer pixels (Agent Surface / tests — bypasses Avalonia hit-test).
    /// </summary>
    public void InjectPointerPressed(float pixelX, float pixelY, PresentationMouseButton button = PresentationMouseButton.Left)
    {
        RefreshPixelSize();
        _mousePixel = new Vector2(pixelX, pixelY);
        if (_mouseDown.Add(button))
            _mousePressedThisFrame.Add(button);
        RaisePointer(ScenePointerPressed, button, isPressed: true);
    }

    /// <summary>Injects a release in framebuffer pixels.</summary>
    public void InjectPointerReleased(float pixelX, float pixelY, PresentationMouseButton button = PresentationMouseButton.Left)
    {
        RefreshPixelSize();
        _mousePixel = new Vector2(pixelX, pixelY);
        if (_mouseDown.Remove(button))
            _mouseReleasedThisFrame.Add(button);
        RaisePointer(ScenePointerReleased, button, isPressed: false);
    }

    /// <summary>Starts the render loop when attached to the visual tree.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _clock = Stopwatch.StartNew();
        _lastSeconds = 0d;
        RequestNextFrameRendering();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        UpdateMouseFromPoint(point.Position);
        foreach (var button in ButtonsFrom(point.Properties, pressed: true))
        {
            if (_mouseDown.Add(button))
                _mousePressedThisFrame.Add(button);
            RaisePointer(ScenePointerPressed, button, isPressed: true);
        }

        e.Handled = true;
        Focus();
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var point = e.GetCurrentPoint(this);
        UpdateMouseFromPoint(point.Position);
        var button = MapButton(e.InitialPressMouseButton);
        if (button is { } b)
        {
            if (_mouseDown.Remove(b))
                _mouseReleasedThisFrame.Add(b);
            RaisePointer(ScenePointerReleased, b, isPressed: false);
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetCurrentPoint(this);
        UpdateMouseFromPoint(point.Position);
        RaisePointer(ScenePointerMoved, PresentationMouseButton.Left, isPressed: point.Properties.IsLeftButtonPressed);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        foreach (var button in _mouseDown)
            _mouseReleasedThisFrame.Add(button);
        _mouseDown.Clear();
    }

    /// <inheritdoc />
    protected override void OnOpenGlInit(GlInterface gl)
    {
        _gl = SilkGlBridge.CreateGl(gl);
        _renderer = new SilkTwoDRenderer(_gl);
    }

    /// <inheritdoc />
    protected override void OnOpenGlRender(GlInterface gl, int framebuffer)
    {
        var scene = Scene;
        if (scene is null || _renderer is null || _gl is null)
            return;

        RefreshPixelSize();
        _renderer.Resize(_pixelWidth, _pixelHeight);

        // Avalonia draws into an FBO — must bind it (drawing FBO 0 is invisible).
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);
        _gl.Viewport(0, 0, (uint)_pixelWidth, (uint)_pixelHeight);

        var now = _clock?.Elapsed.TotalSeconds ?? 0d;
        var delta = (float)System.Math.Max(0d, now - _lastSeconds);
        _lastSeconds = now;
        if (delta > 0f)
        {
            // Edge sets accumulate from Avalonia pointer events between frames.
            FrameUpdating?.Invoke(this, new TwoDFrameEventArgs(delta));
            scene.Update(delta);
            AdvanceMouseFrame();
        }

        _renderer.DrawScene(scene);

        // Keep a CPU PNG for agent capture (RenderTargetBitmap skips GL). Throttle cost.
        if ((++_captureTick & 3) == 0)
        {
            var png = SilkTwoDFramebufferCapture.EncodePng(_gl, _pixelWidth, _pixelHeight);
            if (png.Length > 0)
            {
                lock (_captureGate)
                    _lastPng = png;
            }
        }

        RequestNextFrameRendering();
    }

    /// <inheritdoc />
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _renderer?.Dispose();
        _renderer = null;
        _gl = null;
        lock (_captureGate)
            _lastPng = null;
    }

    void RefreshPixelSize()
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        _pixelWidth = System.Math.Max(1, (int)System.Math.Round(Bounds.Width * scale));
        _pixelHeight = System.Math.Max(1, (int)System.Math.Round(Bounds.Height * scale));
    }

    void UpdateMouseFromPoint(Point controlLocal) =>
        _mousePixel = ToPixelPosition(controlLocal);

    void RaisePointer(EventHandler<TwoDPointerEventArgs>? handler, PresentationMouseButton button, bool isPressed)
    {
        handler?.Invoke(this, new TwoDPointerEventArgs(
            _mousePixel.X,
            _mousePixel.Y,
            _pixelWidth,
            _pixelHeight,
            button,
            isPressed));
    }

    void AdvanceMouseFrame()
    {
        _mousePixelLastFrame = _mousePixel;
        _mousePressedThisFrame.Clear();
        _mouseReleasedThisFrame.Clear();
    }

    static IEnumerable<PresentationMouseButton> ButtonsFrom(PointerPointProperties props, bool pressed)
    {
        if (pressed)
        {
            if (props.IsLeftButtonPressed)
                yield return PresentationMouseButton.Left;
            if (props.IsRightButtonPressed)
                yield return PresentationMouseButton.Right;
            if (props.IsMiddleButtonPressed)
                yield return PresentationMouseButton.Middle;
        }
    }

    static PresentationMouseButton? MapButton(AvaloniaMouseButton button) => button switch
    {
        AvaloniaMouseButton.Left => PresentationMouseButton.Left,
        AvaloniaMouseButton.Right => PresentationMouseButton.Right,
        AvaloniaMouseButton.Middle => PresentationMouseButton.Middle,
        _ => null,
    };
}

/// <summary>Per-frame update args for <see cref="TwoDSceneControl"/>.</summary>
public sealed class TwoDFrameEventArgs(float deltaSeconds) : EventArgs
{
    /// <summary>Elapsed time since the previous frame in seconds.</summary>
    public float DeltaSeconds { get; } = deltaSeconds;
}
