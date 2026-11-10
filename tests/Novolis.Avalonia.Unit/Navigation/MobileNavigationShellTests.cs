using Avalonia.Automation;
using Avalonia.Controls;
using Novolis.Avalonia.Navigation;
using Novolis.Commands.Navigation;

namespace Novolis.Avalonia.Unit.Navigation;

public sealed class MobileNavigationShellTests
{
    [Test]
    public async Task Shell_exposes_native_regions_and_accessible_primary_buttons()
    {
        var navigation = new MobileNavigationController(CreateTree());
        var shell = new MobileNavigationShell(navigation)
        {
            Title = "Reader",
            Body = new TextBlock { Text = "Body" },
        };

        await Assert.That(shell.Title).IsEqualTo("Reader");
        await Assert.That(shell.Body).IsTypeOf<TextBlock>();

        var primary = (Grid)shell.Children[2];
        var buttons = primary.Children.OfType<Button>().ToArray();
        await Assert.That(buttons).Count().IsEqualTo(2);
        await Assert.That(buttons.All(button =>
                button.MinHeight >=
                Novolis.Avalonia.GraphicalProfile.GraphicalProfileColors.TouchTarget))
            .IsTrue();
        await Assert.That(buttons.Select(button =>
                button.GetValue(AutomationProperties.NameProperty)))
            .Contains("Today");
    }

    [Test]
    public async Task Shell_forwards_back_and_renders_secondary_menu()
    {
        var navigation = new MobileNavigationController(CreateTree());
        var shell = new MobileNavigationShell(navigation);

        navigation.Activate("details");
        await Assert.That(shell.TryHandleBack()).IsTrue();
        await Assert.That(navigation.State.CurrentRouteId).IsEqualTo("today");

        await Assert.That(shell.OpenSecondaryMenu()).IsTrue();
        await Assert.That(shell.Children[3].IsVisible).IsTrue();
    }

    [Test]
    public async Task Shell_uses_icon_glyphs_when_three_or_fewer_primary_routes()
    {
        var navigation = new MobileNavigationController(CreateTree());
        var shell = new MobileNavigationShell(navigation);
        var primary = (Grid)shell.Children[2];
        var labels = primary.Children
            .OfType<Button>()
            .Select(button => (button.Content as TextBlock)?.Text ?? string.Empty)
            .ToArray();

        await Assert.That(labels).IsEquivalentTo(new[] { "T", "P" });
        await Assert.That(primary.Children.OfType<Button>().First()
                .GetValue(AutomationProperties.NameProperty))
            .IsEqualTo("Today");
    }

    static MobileMenuTree CreateTree() =>
        new(
        [
            new MobileMenuNode("today", "Today")
            {
                Placement = MobileMenuPlacement.Primary,
                RouteId = "today",
            },
            new MobileMenuNode("places", "Places")
            {
                Placement = MobileMenuPlacement.Primary,
                RouteId = "places",
            },
            new MobileMenuNode("details", "Details")
            {
                RouteId = "details",
            },
            new MobileMenuNode("settings", "Settings", MobileMenuNodeKind.Group)
            {
                Children =
                [
                    new MobileMenuNode("about", "About", MobileMenuNodeKind.Action)
                    {
                        CommandId = "about.open",
                    },
                ],
            },
        ]);
}
