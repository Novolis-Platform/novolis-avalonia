using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Novolis.Avalonia.Agent.Protocol;

namespace Novolis.Avalonia.Agent;

public static class AgentProperties
{
    public static readonly AttachedProperty<string?> IdProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Id", typeof(AgentProperties));

    public static readonly AttachedProperty<string?> RoleProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Role", typeof(AgentProperties));

    public static readonly AttachedProperty<bool> IgnoreProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Ignore", typeof(AgentProperties));

    public static string? GetId(Control control) => control.GetValue(IdProperty);
    public static void SetId(Control control, string? value) => control.SetValue(IdProperty, value);

    public static string? GetRole(Control control) => control.GetValue(RoleProperty);
    public static void SetRole(Control control, string? value) => control.SetValue(RoleProperty, value);

    public static bool GetIgnore(Control control) => control.GetValue(IgnoreProperty);
    public static void SetIgnore(Control control, bool value) => control.SetValue(IgnoreProperty, value);

    public static void SetId(Control control, string id, string? role = null)
    {
        SetId(control, id);
        if (role is not null)
            SetRole(control, role);
        else if (GetRole(control) is null)
            SetRole(control, InferRole(control));
    }

    public static string InferRole(Control control) => control switch
    {
        CheckBox => AgentRoleNames.CheckBox,
        ToggleButton => AgentRoleNames.Toggle,
        Button => AgentRoleNames.Button,
        TextBox => AgentRoleNames.TextBox,
        ListBox => AgentRoleNames.ListBox,
        MenuBase => AgentRoleNames.Menu,
        Window => AgentRoleNames.Window,
        _ => AgentRoleNames.Other
    };
}
