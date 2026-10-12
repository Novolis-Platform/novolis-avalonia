using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Novolis.Rendering.Planar;
using Novolis.Silk;
using PresentationMouseButton = Novolis.Rendering.Presentation.MouseButton;
using AvaloniaMouseButton = global::Avalonia.Input.MouseButton;

namespace Novolis.Avalonia.Rendering;

/// <summary>
/// Avalonia <see cref="OpenGlControlBase"/> that draws a <see cref="PlanarScene"/> via <see cref="GlHost"/>.
/// Exposes DPI-correct framebuffer mouse input for map/game hit-testing.
/// </summary>
/// <remarks>
/// Implements <see cref="ICustomHitTest"/> — OpenGlControlBase has no background, so without this
/// Avalonia never delivers pointer events to the GL surface.
/// </remarks>
public class PlanarSceneControl : OpenGlControlBase, ICustomHitTest
{
    /// <summary>Scene to draw (textures, sprites, HUD, menus).</summary>
    public static readonly StyledProperty<PlanarScene?> SceneProperty =
        AvaloniaProperty.Register<PlanarSceneControl, PlanarScene?>(nameof(Scene));

    private GlHost? _host;
    private Stopwatch? _clock;
    private double _lastSeconds;
    private int _pixelWidth = 1;
    private int _pixelHeight = 1;
    private int _captureTick;
    private bool _captureRequested;
    private readonly object _captureGate = new();
    private byte[]? _lastPng;

    private Vector2 _mousePixel;
    private Vector2 _mousePixelLastFrame;
    private readonly HashSet<PresentationMouseButton> _mouseDown = new();
    private readonly HashSet<PresentationMouseButton> _mousePressedThisFrame = new();
    private readonly HashSet<PresentationMouseButton> _mouseReleasedThisFrame = new();

    /// <summary>Creates a focusable GL viewport that accepts pointer input.</summary>
    public PlanarSceneControl()
    {
        Focusable = true;
        IsHitTestVisible = true;
        // Required with ICustomHitTest so hits stay inside control bounds.
        ClipToBounds = true;
    }

    /// <inheritdoc />
    public bool HitTest(Point point) => true;

    /// <summary>Scene to draw.</summary>
    public PlanarScene? Scene
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
    public event EventHandler<PlanarFrameEventArgs>? FrameUpdating;

    /// <summary>Pointer pressed in framebuffer pixel space (after DPI scale).</summary>
    public event EventHandler<PlanarPointerEventArgs>? ScenePointerPressed;

    /// <summary>Pointer released in framebuffer pixel space.</summary>
    public event EventHandler<PlanarPointerEventArgs>? ScenePointerReleased;

    /// <summary>Pointer moved in framebuffer pixel space.</summary>
    public event EventHandler<PlanarPointerEventArgs>? ScenePointerMoved;

    /// <summary>
    /// Latest PNG of the GL framebuffer (for agent screenshots — <c>RenderTargetBitmap</c> cannot see OpenGL).
    /// </summary>
    public byte[]? TryGetLastFramePng()
    {
        lock (_captureGate)
            return _lastPng is null ? null : (byte[])_lastPng.Clone();
    }

    /// <summary>Requests a PNG capture on the next completed OpenGL frame.</summary>
    public void RequestFrameCapture()
    {
        _captureRequested = true;
        RequestNextFrameRendering();
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
        _host = GlHost.FromProcAddress(name => gl.GetProcAddress(name));
    }

    /// <inheritdoc />
    protected override void OnOpenGlRender(GlInterface gl, int framebuffer)
    {
        var scene = Scene;
        if (scene is null || _host is null)
            return;

        RefreshPixelSize();
        _host.BindFramebuffer(framebuffer);
        _host.Resize(_pixelWidth, _pixelHeight);

        var now = _clock?.Elapsed.TotalSeconds ?? 0d;
        var delta = (float)System.Math.Max(0d, now - _lastSeconds);
        _lastSeconds = now;
        if (delta > 0f)
        {
            // Edge sets accumulate from Avalonia pointer events between frames.
            FrameUpdating?.Invoke(this, new PlanarFrameEventArgs(delta));
            scene.Update(delta);
            AdvanceMouseFrame();
        }

        _host.Submit(scene.Tessellate(_pixelWidth, _pixelHeight));

        var periodicCapture = ++_captureTick % 30 == 0;
        if ((_captureRequested || periodicCapture)
            && _host.TryReadPixels(out var pixels)
            && pixels.Length > 0)
        {
            lock (_captureGate)
                _lastPng = EncodeRgbaPng(pixels, _pixelWidth, _pixelHeight);
            _captureRequested = false;
        }

        RequestNextFrameRendering();
    }

    /// <inheritdoc />
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _host?.Dispose();
        _host = null;
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

    void RaisePointer(EventHandler<PlanarPointerEventArgs>? handler, PresentationMouseButton button, bool isPressed)
    {
        handler?.Invoke(this, new PlanarPointerEventArgs(
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

    static byte[] EncodeRgbaPng(Novolis.Math.Geometry.Rgba32[] pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if (width <= 0 || height <= 0 || pixels.Length != width * height)
        {
            throw new ArgumentException("RGBA frame dimensions do not match the pixel buffer.");
        }

        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        WritePngChunk(output, "IHDR", CreatePngHeader(width, height));

        using var raw = new MemoryStream(width * height * 4 + height);
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                raw.WriteByte(pixel.R);
                raw.WriteByte(pixel.G);
                raw.WriteByte(pixel.B);
                raw.WriteByte(pixel.A);
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(
                   compressed,
                   CompressionLevel.Fastest,
                   leaveOpen: true))
        {
            raw.Position = 0;
            raw.CopyTo(zlib);
        }

        WritePngChunk(output, "IDAT", compressed.ToArray());
        WritePngChunk(output, "IEND", []);
        return output.ToArray();
    }

    static byte[] CreatePngHeader(int width, int height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;
        header[9] = 6;
        return header;
    }

    static void WritePngChunk(Stream destination, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        destination.Write(length);
        destination.Write(typeBytes);
        destination.Write(data);

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, PngCrc32(typeBytes, data));
        destination.Write(crcBytes);
    }

    static uint PngCrc32(byte[] type, byte[] data)
    {
        var crc = 0xffffffffu;
        foreach (var value in type.Concat(data))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xedb88320u
                    : crc >> 1;
            }
        }

        return ~crc;
    }
}
