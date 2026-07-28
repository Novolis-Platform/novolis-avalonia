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

    public static UiSelectResponseDto Select(Window window, UiSelectRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ControlId))
            return new UiSelectResponseDto(request.RequestId, false, "ControlId is required.");

        var target = AgentTreeWalker.FindById(window, request.ControlId);
        if (target is null)
            return new UiSelectResponseDto(request.RequestId, false, $"Control not found: {request.ControlId}");

        if (!target.IsEnabled)
            return new UiSelectResponseDto(request.RequestId, false, "Control is disabled.");

        if (request.Index is null && string.IsNullOrWhiteSpace(request.ItemText))
            return new UiSelectResponseDto(request.RequestId, false, "Provide Index or ItemText.");

        return target switch
        {
            ListBox list => SelectSelectingItemsControl(list, request),
            ComboBox combo => SelectSelectingItemsControl(combo, request),
            TabControl tabs => SelectTabControl(tabs, request),
            _ => new UiSelectResponseDto(
                request.RequestId, false,
                $"Select not supported on {target.GetType().Name} (need ListBox, ComboBox, or TabControl).")
        };
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
                textBox.Text = request.Clear
                    ? request.Text
                    : (textBox.Text ?? string.Empty) + request.Text;
            }
            else if (target is TextBlock)
            {
                return new UiTypeResponseDto(request.RequestId, false, "Cannot type into TextBlock.");
            }
            else if (target is ContentControl { Content: string } contentControl)
            {
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

    private static UiSelectResponseDto SelectSelectingItemsControl(
        SelectingItemsControl control,
        UiSelectRequestDto request)
    {
        var count = control.ItemCount;
        if (count <= 0)
            return new UiSelectResponseDto(request.RequestId, false, "Control has no items.");

        var index = ResolveIndex(control, count, request.Index, request.ItemText);
        if (index is null)
            return new UiSelectResponseDto(request.RequestId, false, "Item not found.");

        control.SelectedIndex = index.Value;
        var text = ItemTextAt(control, index.Value);
        return new UiSelectResponseDto(request.RequestId, true, null, index.Value, text);
    }

    private static UiSelectResponseDto SelectTabControl(TabControl tabs, UiSelectRequestDto request)
    {
        var count = tabs.ItemCount;
        if (count <= 0)
            return new UiSelectResponseDto(request.RequestId, false, "TabControl has no tabs.");

        var index = ResolveIndex(tabs, count, request.Index, request.ItemText, tabHeader: true);
        if (index is null)
            return new UiSelectResponseDto(request.RequestId, false, "Tab not found.");

        tabs.SelectedIndex = index.Value;
        var text = TabHeaderAt(tabs, index.Value);
        return new UiSelectResponseDto(request.RequestId, true, null, index.Value, text);
    }

    private static int? ResolveIndex(
        ItemsControl control,
        int count,
        int? index,
        string? itemText,
        bool tabHeader = false)
    {
        if (index is int i)
        {
            if (i < 0 || i >= count)
                return null;
            return i;
        }

        if (string.IsNullOrWhiteSpace(itemText))
            return null;

        for (var n = 0; n < count; n++)
        {
            var text = tabHeader && control is TabControl tabs
                ? TabHeaderAt(tabs, n)
                : ItemTextAt(control, n);
            if (text is not null
                && text.Contains(itemText, StringComparison.OrdinalIgnoreCase))
            {
                return n;
            }
        }

        return null;
    }

    private static string? ItemTextAt(ItemsControl control, int index)
    {
        var item = control.Items[index];
        return item switch
        {
            null => null,
            string s => s,
            HeaderedContentControl { Header: string h } => h,
            HeaderedContentControl hc => hc.Header?.ToString(),
            _ => item.ToString()
        };
    }

    private static string? TabHeaderAt(TabControl tabs, int index)
    {
        var item = tabs.Items[index];
        return item switch
        {
            TabItem { Header: string h } => h,
            TabItem ti => ti.Header?.ToString(),
            string s => s,
            _ => item?.ToString()
        };
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
