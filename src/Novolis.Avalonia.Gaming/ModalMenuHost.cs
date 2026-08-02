using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Gaming;

/// <summary>
/// Dimmed full-bleed host for pause / encyclopedia / options menus over a game viewport.
/// Hit-testable while visible so clicks do not fall through to the Silk surface.
/// </summary>
public sealed class ModalMenuHost : Border
{
    readonly ContentControl _body = new()
    {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>Raised when the dimmer is clicked (outside the body) or Escape is pressed while focused.</summary>
    public event EventHandler? DismissRequested;

    /// <summary>Creates a hidden modal host.</summary>
    public ModalMenuHost()
    {
        Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
        IsVisible = false;
        IsHitTestVisible = true;
        Focusable = true;
        Child = _body;
        PointerPressed += OnPointerPressed;
        KeyDown += OnKeyDown;
    }

    /// <summary>Menu content shown centered over the dimmer.</summary>
    public Control? Body
    {
        get => _body.Content as Control;
        set => _body.Content = value;
    }

    /// <summary>Shows <paramref name="content"/> and focuses the host.</summary>
    public void Show(Control content)
    {
        Body = content;
        IsVisible = true;
        Focus();
    }

    /// <summary>Hides the modal and clears body content.</summary>
    public void Dismiss()
    {
        IsVisible = false;
        Body = null;
    }

    void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source == this)
            DismissRequested?.Invoke(this, EventArgs.Empty);
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DismissRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
