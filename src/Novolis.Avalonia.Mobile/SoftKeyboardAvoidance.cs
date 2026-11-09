using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Novolis.Avalonia.Mobile;

/// <summary>
/// Squishes a <see cref="TemplatedControl"/> by bottom padding when the platform input pane opens,
/// then invokes a caret-into-view callback. Never overlays content on the keyboard.
/// </summary>
public sealed class SoftKeyboardAvoidance : IDisposable
{
    readonly TemplatedControl _host;
    readonly Action? _ensureCaretVisible;
    readonly Thickness _basePadding;
    IInputPane? _inputPane;
    bool _attached;
    bool _disposed;
    double _keyboardBottom;

    /// <summary>
    /// Creates avoidance for <paramref name="host"/>. Padding is applied to the host
    /// so children reflow (squish). <paramref name="ensureCaretVisible"/> runs after each adjust.
    /// </summary>
    public SoftKeyboardAvoidance(TemplatedControl host, Action? ensureCaretVisible = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _ensureCaretVisible = ensureCaretVisible;
        _basePadding = host.Padding;
        _host.AttachedToVisualTree += OnAttached;
        _host.DetachedFromVisualTree += OnDetached;
        if (_host.IsLoaded)
            Attach();
    }

    /// <summary>Current keyboard bottom inset applied to <see cref="Host"/>.</summary>
    public double KeyboardBottom => _keyboardBottom;

    /// <summary>Host that receives bottom padding while the keyboard is open.</summary>
    public TemplatedControl Host => _host;

    /// <summary>
    /// Applies a synthetic keyboard height (desktop / headless proof). Pass 0 to clear.
    /// </summary>
    public void SimulateKeyboardBottom(double height) =>
        ApplyInset(global::System.Math.Max(0, height));

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Detach();
        _host.AttachedToVisualTree -= OnAttached;
        _host.DetachedFromVisualTree -= OnDetached;
        _host.Padding = _basePadding;
    }

    void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Attach();

    void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) => Detach();

    void Attach()
    {
        if (_attached || _disposed)
            return;

        var top = TopLevel.GetTopLevel(_host);
        _inputPane = top?.InputPane;
        if (_inputPane is not null)
            _inputPane.StateChanged += OnInputPaneStateChanged;

        _host.SizeChanged += OnHostSizeChanged;
        _attached = true;
        SyncFromPane();
    }

    void Detach()
    {
        if (!_attached)
            return;

        if (_inputPane is not null)
            _inputPane.StateChanged -= OnInputPaneStateChanged;
        _inputPane = null;
        _host.SizeChanged -= OnHostSizeChanged;
        _attached = false;
    }

    void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        var rect = e.NewState == InputPaneState.Open ? e.EndRect : default;
        ApplyInset(SoftKeyboardLayout.BottomInset(rect, ResolveHostHeight()));
    }

    void OnHostSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (_keyboardBottom > 0 || _inputPane?.State == InputPaneState.Open)
            RequestCaretVisible();
    }

    void SyncFromPane()
    {
        if (_inputPane is null)
            return;
        var inset = _inputPane.State == InputPaneState.Open
            ? SoftKeyboardLayout.BottomInset(_inputPane.OccludedRect, ResolveHostHeight())
            : 0;
        ApplyInset(inset);
    }

    double ResolveHostHeight()
    {
        if (_host.Bounds.Height > 0)
            return _host.Bounds.Height;
        return TopLevel.GetTopLevel(_host)?.Bounds.Height ?? 0;
    }

    void ApplyInset(double keyboardBottom)
    {
        _keyboardBottom = global::System.Math.Max(0, keyboardBottom);
        _host.Padding = SoftKeyboardLayout.WithKeyboardBottom(_basePadding, _keyboardBottom);
        RequestCaretVisible();
    }

    void RequestCaretVisible()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                _host.UpdateLayout();
                _ensureCaretVisible?.Invoke();
            },
            DispatcherPriority.Loaded);
    }
}
