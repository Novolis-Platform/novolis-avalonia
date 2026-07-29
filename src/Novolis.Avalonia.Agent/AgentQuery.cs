using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Novolis.Avalonia.Agent.Protocol;
using Novolis.Avalonia.Agent.Protocol.Dto;

namespace Novolis.Avalonia.Agent;

internal static class AgentQuery
{
    public static UiGetResponseDto Get(Window window, UiGetRequestDto request)
    {
        var ids = request.ControlIds ?? Array.Empty<string>();
        var controls = new UiControlStateDto[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (string.IsNullOrWhiteSpace(id))
            {
                controls[i] = new UiControlStateDto("", false, false, false, null, null, null);
                continue;
            }

            var control = AgentTreeWalker.FindById(window, id);
            if (control is null)
            {
                controls[i] = new UiControlStateDto(id, false, false, false, null, null, null);
                continue;
            }

            controls[i] = new UiControlStateDto(
                id,
                true,
                control.IsEnabled,
                control.IsVisible,
                AgentTreeWalker.DescribeText(control),
                AgentProperties.GetRole(control) ?? AgentProperties.InferRole(control),
                control.GetType().Name);
        }

        return new UiGetResponseDto(
            request.RequestId,
            true,
            null,
            controls,
            window.Title,
            Environment.ProcessId);
    }

    public static UiItemsResponseDto Items(Window window, UiItemsRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ControlId))
            return new UiItemsResponseDto(request.RequestId, false, "ControlId is required.", "", null, null, Array.Empty<UiItemDto>());

        var target = AgentTreeWalker.FindById(window, request.ControlId);
        if (target is null)
            return new UiItemsResponseDto(request.RequestId, false, $"Control not found: {request.ControlId}", request.ControlId, null, null, Array.Empty<UiItemDto>());

        return target switch
        {
            ListBox list => ItemsFromSelecting(list, request, "listbox"),
            ComboBox combo => ItemsFromSelecting(combo, request, "combobox"),
            TabControl tabs => ItemsFromTabs(tabs, request),
            _ => new UiItemsResponseDto(
                request.RequestId, false,
                $"Items not supported on {target.GetType().Name} (need ListBox, ComboBox, or TabControl).",
                request.ControlId, null, null, Array.Empty<UiItemDto>())
        };
    }

    private static UiItemsResponseDto ItemsFromSelecting(
        SelectingItemsControl control,
        UiItemsRequestDto request,
        string kind)
    {
        var count = control.ItemCount;
        var items = new UiItemDto[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = new UiItemDto(i, AgentInput.ItemTextAt(control, i) ?? "", control.SelectedIndex == i);
        }

        return new UiItemsResponseDto(
            request.RequestId, true, null, request.ControlId, kind, control.SelectedIndex, items);
    }

    private static UiItemsResponseDto ItemsFromTabs(TabControl tabs, UiItemsRequestDto request)
    {
        var count = tabs.ItemCount;
        var items = new UiItemDto[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = new UiItemDto(i, AgentInput.TabHeaderAt(tabs, i) ?? "", tabs.SelectedIndex == i);
        }

        return new UiItemsResponseDto(
            request.RequestId, true, null, request.ControlId, "tabcontrol", tabs.SelectedIndex, items);
    }
}
