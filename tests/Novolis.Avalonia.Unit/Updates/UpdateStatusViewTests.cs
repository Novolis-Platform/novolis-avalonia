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
}
