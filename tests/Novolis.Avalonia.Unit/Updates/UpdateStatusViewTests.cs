using Avalonia.Automation;
using Avalonia.Controls;
using Novolis.Avalonia.Updates;

namespace Novolis.Avalonia.Unit.Updates;

public sealed class UpdateStatusViewTests
{
    [Test]
    public async Task View_exposes_stable_automation_identity_and_profile_card()
    {
        var view = new UpdateStatusView();

        await Assert.That(view.GetValue(AutomationProperties.AutomationIdProperty))
            .IsEqualTo("UpdateStatusView");
        await Assert.That(view.GetValue(AutomationProperties.NameProperty))
            .IsEqualTo("Application updates");
        await Assert.That(view.ShowInline).IsTrue();
        await Assert.That(view.NotificationMode).IsEqualTo(UpdateNotificationMode.Inline);
        await Assert.That(view.Content).IsTypeOf<Border>();
        await Assert.That(((Border)view.Content!).Classes).Contains("ngp-card");
    }

    [Test]
    public async Task View_can_switch_between_inline_and_prominent_notification_modes()
    {
        var view = new UpdateStatusView
        {
            ShowInline = false,
            NotificationMode = UpdateNotificationMode.Popup,
        };

        await Assert.That(view.IsVisible).IsFalse();
        await Assert.That(view.NotificationMode).IsEqualTo(UpdateNotificationMode.Popup);
        await Assert.That(view.Content).IsNotNull();
    }

    [Test]
    public async Task View_exposes_all_user_actions_with_stable_accessibility_ids()
    {
        var view = new UpdateStatusView();
        var card = (Border)view.Content!;
        var content = (StackPanel)card.Child!;
        var actions = content.Children.OfType<StackPanel>().Single();
        var buttons = actions.Children.OfType<Button>().ToList();

        await Assert.That(buttons).Count().IsEqualTo(6);
        await Assert.That(buttons.Select(button =>
                button.GetValue(AutomationProperties.AutomationIdProperty)))
            .Contains("UpdateStatusView.ReleaseButton");
        await Assert.That(buttons.Select(button =>
                button.GetValue(AutomationProperties.AutomationIdProperty)))
            .Contains("UpdateStatusView.DownloadButton");
        await Assert.That(buttons.Select(button =>
                button.GetValue(AutomationProperties.AutomationIdProperty)))
            .Contains("UpdateStatusView.ApplyButton");
        await Assert.That(buttons.All(button =>
                !string.IsNullOrWhiteSpace(
                    button.GetValue(AutomationProperties.NameProperty) as string)))
            .IsTrue();
    }
}
