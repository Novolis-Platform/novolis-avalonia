using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Mouse, keyboard, and assistive front door for the common no-code actions.
/// </summary>
public sealed class AssetStudioActionBar : UserControl
{
    private static readonly (string Label, string Command)[] Actions =
    [
        ("New Material", "new material"),
        ("New Asset", "new asset"),
        ("Templates", "open template lightsaber-blade"),
        ("Instance", "create instance"),
        ("Save Preset", "save preset"),
        ("Validate", "validate"),
        ("Preview", "preview"),
        ("Build", "build"),
        ("Export", "export"),
        ("Help", "help"),
    ];

    public AssetStudioActionBar()
    {
        var row = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemSpacing = 4,
            LineSpacing = 4,
            Margin = new Thickness(6, 4),
        };
        foreach (var action in Actions)
        {
            var button = new Button
            {
                Content = action.Label,
                Padding = new Thickness(8, 4),
                Tag = action.Command,
            };
            ProfileBinding.Bind(
                button,
                Button.BackgroundProperty,
                Profile.RaisedResourceKey);
            ProfileBinding.Bind(
                button,
                Button.ForegroundProperty,
                Profile.TextResourceKey);
            button.SetValue(
                AutomationProperties.NameProperty,
                action.Label);
            button.SetValue(
                AutomationProperties.AutomationIdProperty,
                "assetstudio.action." +
                action.Command.Replace(' ', '-'));
            button.Click += (_, _) =>
            {
                if (button.Tag is string command)
                {
                    ActionRequested?.Invoke(
                        this,
                        new AssetStudioActionRequestedEventArgs(
                            command));
                }
            };
            row.Children.Add(button);
        }

        ProfileBinding.Bind(
            this,
            BackgroundProperty,
            Profile.SurfaceResourceKey);
        Content = row;
    }

    public event EventHandler<AssetStudioActionRequestedEventArgs>?
        ActionRequested;
}
