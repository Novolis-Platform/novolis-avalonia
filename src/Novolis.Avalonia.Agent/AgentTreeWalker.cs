using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Novolis.Avalonia.Agent.Protocol;
using Novolis.Avalonia.Agent.Protocol.Dto;

namespace Novolis.Avalonia.Agent;

internal static class AgentTreeWalker
{
    public static UiTreeNodeDto[] Collect(Window window, bool interactiveOnly)
    {
        var nodes = new List<UiTreeNodeDto>();
        Walk(window, window, "Window", interactiveOnly, nodes);
        return nodes.ToArray();
    }

    public static Control? FindById(Window window, string id)
    {
        Control? found = null;
        WalkFind(window, window, "Window", id, ref found);
        return found;
    }

    public static Control? HitTest(Window window, double x, double y)
    {
        Control? best = null;
        var bestArea = double.MaxValue;
        foreach (var control in window.GetVisualDescendants().OfType<Control>())
        {
            if (!control.IsVisible || AgentProperties.GetIgnore(control))
                continue;
            var bounds = GetWindowBounds(window, control);
            if (x < bounds.X || y < bounds.Y || x > bounds.X + bounds.Width || y > bounds.Y + bounds.Height)
                continue;
            var area = bounds.Width * bounds.Height;
            if (area < bestArea && area > 0)
            {
                bestArea = area;
                best = control;
            }
        }

        return best;
    }

    /// <summary>Human-readable text for agents (includes list/combo item summaries).</summary>
    public static string? DescribeText(Control control)
    {
        var baseText = GetText(control);
        if (control is ListBox or ComboBox or TabControl)
        {
            var summary = SummarizeItems(control);
            if (summary is null)
                return baseText;
            return string.IsNullOrWhiteSpace(baseText) ? summary : $"{baseText} · {summary}";
        }

        return baseText;
    }

    private static string? SummarizeItems(Control control)
    {
        try
        {
            return control switch
            {
                ListBox list => FormatItems("list", list.ItemCount, list.SelectedIndex, i => AgentInput.ItemTextAt(list, i)),
                ComboBox combo => FormatItems("combo", combo.ItemCount, combo.SelectedIndex, i => AgentInput.ItemTextAt(combo, i)),
                TabControl tabs => FormatItems("tabs", tabs.ItemCount, tabs.SelectedIndex, i => AgentInput.TabHeaderAt(tabs, i)),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string FormatItems(string kind, int count, int selected, Func<int, string?> textAt)
    {
        if (count <= 0)
            return $"{kind}:0";

        var take = Math.Min(count, 12);
        var parts = new List<string>(take);
        for (var i = 0; i < take; i++)
        {
            var t = textAt(i) ?? "";
            if (t.Length > 80)
                t = t[..77] + "...";
            var mark = i == selected ? "*" : "";
            parts.Add($"[{i}{mark}]{t}");
        }

        var more = count > take ? $"; +{count - take} more" : "";
        return $"{kind}:{count} {string.Join(" | ", parts)}{more}";
    }

    private static void Walk(Window window, Control control, string path, bool interactiveOnly, List<UiTreeNodeDto> nodes)
    {
        if (AgentProperties.GetIgnore(control))
            return;

        var include = !interactiveOnly || IsInteractive(control) || AgentProperties.GetId(control) is not null;
        if (include)
        {
            var bounds = GetWindowBounds(window, control);
            var id = ResolveId(control, path);
            var role = AgentProperties.GetRole(control) ?? AgentProperties.InferRole(control);
            var focused = TopLevel.GetTopLevel(control)?.FocusManager?.GetFocusedElement() == control;
            nodes.Add(new UiTreeNodeDto(
                id,
                role,
                control.GetType().Name,
                bounds,
                control.IsEnabled,
                control.IsVisible,
                focused,
                DescribeText(control),
                path));

            // Emit compact list-item rows under agent-tagged lists so agents can read options without Select.
            if (AgentProperties.GetId(control) is not null)
                AppendItemNodes(window, control, id, path, nodes);
        }

        var siblingCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var child in control.GetVisualChildren().OfType<Control>())
        {
            var typeName = child.GetType().Name;
            siblingCounts.TryGetValue(typeName, out var index);
            siblingCounts[typeName] = index + 1;
            Walk(window, child, $"{path}/{typeName}[{index}]", interactiveOnly, nodes);
        }
    }

    private static void AppendItemNodes(
        Window window,
        Control control,
        string parentId,
        string path,
        List<UiTreeNodeDto> nodes)
    {
        try
        {
            if (control is ListBox list)
            {
                for (var i = 0; i < list.ItemCount && i < 64; i++)
                {
                    var text = AgentInput.ItemTextAt(list, i) ?? "";
                    nodes.Add(new UiTreeNodeDto(
                        $"{parentId}[{i}]",
                        AgentRoleNames.ListItem,
                        "ListItem",
                        GetWindowBounds(window, list),
                        list.IsEnabled,
                        list.IsVisible,
                        list.SelectedIndex == i,
                        text,
                        $"{path}/Item[{i}]"));
                }
            }
            else if (control is ComboBox combo)
            {
                for (var i = 0; i < combo.ItemCount && i < 64; i++)
                {
                    var text = AgentInput.ItemTextAt(combo, i) ?? "";
                    nodes.Add(new UiTreeNodeDto(
                        $"{parentId}[{i}]",
                        AgentRoleNames.ListItem,
                        "ComboItem",
                        GetWindowBounds(window, combo),
                        combo.IsEnabled,
                        combo.IsVisible,
                        combo.SelectedIndex == i,
                        text,
                        $"{path}/Item[{i}]"));
                }
            }
            else if (control is TabControl tabs)
            {
                for (var i = 0; i < tabs.ItemCount && i < 32; i++)
                {
                    var text = AgentInput.TabHeaderAt(tabs, i) ?? "";
                    nodes.Add(new UiTreeNodeDto(
                        $"{parentId}[{i}]",
                        "tab",
                        "TabItem",
                        GetWindowBounds(window, tabs),
                        tabs.IsEnabled,
                        tabs.IsVisible,
                        tabs.SelectedIndex == i,
                        text,
                        $"{path}/Tab[{i}]"));
                }
            }
        }
        catch
        {
            // ignore item enumeration faults
        }
    }

    private static void WalkFind(Window window, Control control, string path, string id, ref Control? found)
    {
        if (found is not null || AgentProperties.GetIgnore(control))
            return;

        if (string.Equals(ResolveId(control, path), id, StringComparison.Ordinal))
        {
            found = control;
            return;
        }

        var siblingCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var child in control.GetVisualChildren().OfType<Control>())
        {
            var typeName = child.GetType().Name;
            siblingCounts.TryGetValue(typeName, out var index);
            siblingCounts[typeName] = index + 1;
            WalkFind(window, child, $"{path}/{typeName}[{index}]", id, ref found);
            if (found is not null)
                return;
        }
    }

    private static bool IsInteractive(Control control) =>
        control is Button or TextBox or ListBox or CheckBox or ToggleButton or MenuBase or ComboBox
            or TabControl or Slider or NumericUpDown or TabItem;

    private static string ResolveId(Control control, string path)
    {
        var attached = AgentProperties.GetId(control);
        if (!string.IsNullOrWhiteSpace(attached))
            return attached!;

        var automation = AutomationProperties.GetName(control);
        if (!string.IsNullOrWhiteSpace(automation))
            return automation!;

        if (!string.IsNullOrWhiteSpace(control.Name))
            return control.Name!;

        return path;
    }

    private static UiBoundsDto GetWindowBounds(Window window, Control control)
    {
        var topLeft = control.TranslatePoint(new Point(0, 0), window) ?? new Point(0, 0);
        return new UiBoundsDto(topLeft.X, topLeft.Y, control.Bounds.Width, control.Bounds.Height);
    }

    private static string? GetText(Control control) => control switch
    {
        TextBox tb => tb.Text,
        TextBlock block => block.Text,
        ContentControl { Content: string s } => s,
        ContentControl { Content: TextBlock tb } => tb.Text,
        ContentControl cc => cc.Content?.ToString(),
        _ => null
    };
}
