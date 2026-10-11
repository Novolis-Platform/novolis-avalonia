using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Shared profile-bound card shell for Asset Studio editor panels.
/// </summary>
public abstract class AssetStudioPanel : Border
{
    protected AssetStudioPanel(string title, Control content, string automationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);

        PanelTitle = title;
        SetValue(AutomationProperties.NameProperty, title);
        SetValue(AutomationProperties.AutomationIdProperty, automationId);
        Classes.Add("ngp-card");

        ProfileBinding.Bind(
            this,
            BackgroundProperty,
            Profile.SurfaceResourceKey);
        ProfileBinding.Bind(
            this,
            BorderBrushProperty,
            Profile.BorderResourceKey);

        var heading = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(12, 10, 12, 6),
        };
        ProfileBinding.Bind(
            heading,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey);

        content.Margin = new Thickness(12, 0, 12, 12);
        var body = new DockPanel();
        DockPanel.SetDock(heading, Dock.Top);
        body.Children.Add(heading);
        body.Children.Add(content);
        Child = body;
    }

    /// <summary>Panel heading shown above the panel content.</summary>
    public string PanelTitle { get; }

    /// <summary>Applies the profile body color to a text control.</summary>
    protected static void BindText(Control control, string resourceKey) =>
        ProfileBinding.Bind(
            control,
            TextBlock.ForegroundProperty,
            resourceKey);

    /// <summary>Applies the profile surface color to a control background.</summary>
    protected static void BindSurface(Control control) =>
        ProfileBinding.Bind(
            control,
            Panel.BackgroundProperty,
            Profile.SurfaceResourceKey);

    /// <summary>Applies the profile raised color to a control background.</summary>
    protected static void BindRaised(Control control) =>
        ProfileBinding.Bind(
            control,
            Panel.BackgroundProperty,
            Profile.RaisedResourceKey);
}
