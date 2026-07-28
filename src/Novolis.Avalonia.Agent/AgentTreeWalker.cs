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
                GetText(control),
                path));
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
