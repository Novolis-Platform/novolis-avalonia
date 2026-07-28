using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Novolis.Avalonia.Agent.Protocol.Dto;

namespace Novolis.Avalonia.Agent;

internal static class AgentInput
{
    public static UiClickResponseDto Click(Window window, UiClickRequestDto request)
    {
        Control? target = null;
        string? clickedId = null;

        if (!string.IsNullOrWhiteSpace(request.ControlId))
        {
            target = AgentTreeWalker.FindById(window, request.ControlId);
            if (target is null)
                return new UiClickResponseDto(request.RequestId, false, $"Control not found: {request.ControlId}", null);
            clickedId = request.ControlId;
        }
        else if (request.X is double x && request.Y is double y)
        {
            target = AgentTreeWalker.HitTest(window, x, y);
            if (target is null)
                return new UiClickResponseDto(request.RequestId, false, $"No control at ({x}, {y}).", null);
            clickedId = AgentProperties.GetId(target) ?? target.Name ?? target.GetType().Name;
        }
        else
        {
            return new UiClickResponseDto(request.RequestId, false, "Provide ControlId or X/Y.", null);
        }

        if (!target.IsEnabled)
            return new UiClickResponseDto(request.RequestId, false, "Control is disabled.", clickedId);

        switch (target)
        {
            case CheckBox check:
                check.IsChecked = !(check.IsChecked ?? false);
                break;
            case ToggleButton toggle:
                toggle.IsChecked = !(toggle.IsChecked ?? false);
                break;
            case Button button:
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                break;
            default:
                target.Focus();
                break;
        }

        return new UiClickResponseDto(request.RequestId, true, null, clickedId);
    }

    public static UiTypeResponseDto Type(Window window, UiTypeRequestDto request)
    {
        Control? target = null;
        if (!string.IsNullOrWhiteSpace(request.ControlId))
        {
            target = AgentTreeWalker.FindById(window, request.ControlId);
            if (target is null)
                return new UiTypeResponseDto(request.RequestId, false, $"Control not found: {request.ControlId}");
        }
        else
        {
            target = TopLevel.GetTopLevel(window)?.FocusManager?.GetFocusedElement() as Control;
        }

        if (target is null)
            return new UiTypeResponseDto(request.RequestId, false, "No focus target.");

        target.Focus();

        if (request.Text is not null)
        {
            if (target is TextBox textBox)
            {
                textBox.Text = (textBox.Text ?? string.Empty) + request.Text;
            }
            else if (target is TextBlock)
            {
                return new UiTypeResponseDto(request.RequestId, false, "Cannot type into TextBlock.");
            }
            else
            {
                // Best-effort: set Content if it is a string ContentControl without a TextBox.
                if (target is ContentControl { Content: string } contentControl)
                    contentControl.Content = request.Text;
            }
        }

        if (request.Keys is { Length: > 0 })
        {
            foreach (var keyName in request.Keys)
            {
                if (!TryParseKey(keyName, out var key))
                    return new UiTypeResponseDto(request.RequestId, false, $"Unknown key: {keyName}");

                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = key
                });
                target.RaiseEvent(new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyUpEvent,
                    Key = key
                });
            }
        }

        return new UiTypeResponseDto(request.RequestId, true, null);
    }

    private static bool TryParseKey(string name, out Key key)
    {
        key = Key.None;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (Enum.TryParse(name, ignoreCase: true, out key) && key != Key.None)
            return true;

        key = name.Trim().ToLowerInvariant() switch
        {
            "enter" or "return" => Key.Enter,
            "tab" => Key.Tab,
            "escape" or "esc" => Key.Escape,
            "space" => Key.Space,
            "backspace" => Key.Back,
            "delete" or "del" => Key.Delete,
            "up" => Key.Up,
            "down" => Key.Down,
            "left" => Key.Left,
            "right" => Key.Right,
            _ => Key.None
        };
        return key != Key.None;
    }
}
