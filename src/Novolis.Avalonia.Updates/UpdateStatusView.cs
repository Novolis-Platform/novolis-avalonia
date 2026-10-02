using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Novolis.Registry.Primitives.Updates;
using Novolis.Registry.Updates;

namespace Novolis.Avalonia.Updates;

/// <summary>
/// Code-only, profile-bound update surface. Product hosts provide the
/// notification and platform file/installer seams.
/// </summary>
public sealed class UpdateStatusView : UserControl
{
    public static readonly StyledProperty<UpdateCoordinator?> CoordinatorProperty =
        AvaloniaProperty.Register<UpdateStatusView, UpdateCoordinator?>(nameof(Coordinator));

    public static readonly StyledProperty<IUpdateHostActions?> HostActionsProperty =
        AvaloniaProperty.Register<UpdateStatusView, IUpdateHostActions?>(nameof(HostActions));

    public static readonly StyledProperty<UpdateNotificationMode> NotificationModeProperty =
        AvaloniaProperty.Register<UpdateStatusView, UpdateNotificationMode>(
            nameof(NotificationMode),
            UpdateNotificationMode.Inline);

    public static readonly StyledProperty<bool> ShowInlineProperty =
        AvaloniaProperty.Register<UpdateStatusView, bool>(nameof(ShowInline), true);

    private readonly TextBlock _status = CreateText("Status", "UpdateStatusView.StatusText");
    private readonly TextBlock _currentVersion = CreateText("Current version", "UpdateStatusView.CurrentVersion");
    private readonly TextBlock _candidateVersion = CreateText("No update available", "UpdateStatusView.CandidateVersion");
    private readonly TextBlock _releaseNotes = CreateText("", "UpdateStatusView.ReleaseNotes");
    private readonly TextBlock _error = CreateText("", "UpdateStatusView.ErrorText");
    private readonly ProgressBar _progress = new()
    {
        Minimum = 0,
        Maximum = 100,
        IsVisible = false,
        Height = 6,
    };
    private readonly Button _checkButton = CreateButton("Check for updates", "UpdateStatusView.CheckButton");
    private readonly Button _releaseButton = CreateButton("View release", "UpdateStatusView.ReleaseButton");
    private readonly Button _downloadButton = CreateButton("Download", "UpdateStatusView.DownloadButton");
    private readonly Button _snoozeButton = CreateButton("Later", "UpdateStatusView.SnoozeButton");
    private readonly Button _revealButton = CreateButton("Show downloaded file", "UpdateStatusView.RevealButton");
    private readonly Button _applyButton = CreateButton("Continue installation", "UpdateStatusView.ApplyButton");
    private int _notificationInFlight;
    private UpdateCoordinator? _subscribedCoordinator;

    /// <summary>Creates the profile-bound update status surface.</summary>
    public UpdateStatusView()
    {
        SetValue(AutomationProperties.NameProperty, "Application updates");
        SetValue(AutomationProperties.AutomationIdProperty, "UpdateStatusView");
        _progress.SetValue(AutomationProperties.NameProperty, "Update download progress");
        _progress.SetValue(AutomationProperties.AutomationIdProperty, "UpdateStatusView.Progress");

        _status.Classes.Add("ngp-body");
        _currentVersion.Classes.Add("ngp-body");
        _candidateVersion.Classes.Add("ngp-page-title");
        _releaseNotes.Classes.Add("ngp-body");
        _error.Classes.Add("ngp-body");
        GraphicalProfileBinding.Bind(_error, TextBlock.ForegroundProperty, GraphicalProfile.DangerResourceKey);
        _releaseNotes.TextWrapping = TextWrapping.Wrap;
        _error.TextWrapping = TextWrapping.Wrap;

        _checkButton.Classes.Add("nav-button");
        _releaseButton.Classes.Add("nav-button");
        _downloadButton.Classes.Add("ngp-action-button");
        _snoozeButton.Classes.Add("nav-button");
        _revealButton.Classes.Add("nav-button");
        _applyButton.Classes.Add("ngp-accent-button");

        _checkButton.Click += async (_, _) => await CheckAsync().ConfigureAwait(true);
        _releaseButton.Click += async (_, _) => await OpenReleaseAsync().ConfigureAwait(true);
        _downloadButton.Click += async (_, _) => await DownloadAsync().ConfigureAwait(true);
        _snoozeButton.Click += async (_, _) => await SnoozeAsync().ConfigureAwait(true);
        _revealButton.Click += async (_, _) => await RevealAsync().ConfigureAwait(true);
        _applyButton.Click += async (_, _) => await ApplyAsync().ConfigureAwait(true);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                _checkButton,
                _releaseButton,
                _downloadButton,
                _snoozeButton,
                _revealButton,
                _applyButton,
            },
        };
        var content = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                _candidateVersion,
                _status,
                _currentVersion,
                _releaseNotes,
                _progress,
                _error,
                actions,
            },
        };
        var card = new Border
        {
            Padding = new Thickness(16),
            Child = content,
        };
        card.Classes.Add("ngp-card");
        GraphicalProfileBinding.Bind(card, Border.BackgroundProperty, GraphicalProfile.SurfaceResourceKey);
        GraphicalProfileBinding.Bind(card, Border.BorderBrushProperty, GraphicalProfile.BorderResourceKey);
        Content = card;
        IsVisible = ShowInline;
    }

    /// <summary>Coordinator supplying the neutral update snapshot.</summary>
    public UpdateCoordinator? Coordinator
    {
        get => GetValue(CoordinatorProperty);
        set => SetValue(CoordinatorProperty, value);
    }

    /// <summary>Host callbacks for notifications and file/installer actions.</summary>
    public IUpdateHostActions? HostActions
    {
        get => GetValue(HostActionsProperty);
        set => SetValue(HostActionsProperty, value);
    }

    /// <summary>Notification surface used for a newly discovered candidate.</summary>
    public UpdateNotificationMode NotificationMode
    {
        get => GetValue(NotificationModeProperty);
        set => SetValue(NotificationModeProperty, value);
    }

    /// <summary>Whether the inline status card is visible.</summary>
    public bool ShowInline
    {
        get => GetValue(ShowInlineProperty);
        set => SetValue(ShowInlineProperty, value);
    }

    /// <summary>Applies profile dynamic resources to the card again after host theme changes.</summary>
    public void ReapplyProfileResources()
    {
        if (Content is Border card)
        {
            GraphicalProfileBinding.Bind(card, Border.BackgroundProperty, GraphicalProfile.SurfaceResourceKey);
            GraphicalProfileBinding.Bind(card, Border.BorderBrushProperty, GraphicalProfile.BorderResourceKey);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CoordinatorProperty)
        {
            Unsubscribe((UpdateCoordinator?)change.OldValue);
            var coordinator = change.NewValue as UpdateCoordinator;
            Subscribe(coordinator);
            Render(coordinator?.Snapshot ?? new UpdateSnapshot
            {
                CurrentVersion = "unknown",
            });
        }
        else if (change.Property == ShowInlineProperty)
        {
            IsVisible = ShowInline;
        }
    }

    private void Subscribe(UpdateCoordinator? coordinator)
    {
        if (coordinator is null)
            return;
        _subscribedCoordinator = coordinator;
        coordinator.SnapshotChanged += OnSnapshotChanged;
        Render(coordinator.Snapshot);
    }

    private void Unsubscribe(UpdateCoordinator? coordinator)
    {
        if (coordinator is null || !ReferenceEquals(_subscribedCoordinator, coordinator))
            return;
        coordinator.SnapshotChanged -= OnSnapshotChanged;
        _subscribedCoordinator = null;
    }

    private void OnSnapshotChanged(UpdateSnapshot snapshot)
    {
        if (Dispatcher.UIThread.CheckAccess())
            Render(snapshot);
        else
            Dispatcher.UIThread.Post(() => Render(snapshot));
    }

    private void Render(UpdateSnapshot snapshot)
    {
        _status.Text = snapshot.State switch
        {
            UpdateState.Available => "An update is available",
            UpdateState.Downloading => "Downloading update…",
            UpdateState.DownloadReady => "Download ready",
            UpdateState.Deferred => "Update deferred",
            UpdateState.UpToDate => "You are up to date",
            UpdateState.Failed => snapshot.ErrorMessage ?? "Update check failed",
            UpdateState.Checking => "Checking for updates…",
            _ => "Updates",
        };
        _currentVersion.Text = $"Current version: {snapshot.CurrentVersion}";
        _candidateVersion.Text = snapshot.Candidate is { } candidate
            ? $"Version {candidate.Version} · {candidate.Manifest.Channel}"
            : "No update available";
        _releaseNotes.Text = snapshot.Candidate?.ReleaseNotes ?? "";
        _releaseNotes.IsVisible = !string.IsNullOrWhiteSpace(_releaseNotes.Text);
        _error.Text = snapshot.ErrorMessage ?? "";
        _error.IsVisible = !string.IsNullOrWhiteSpace(_error.Text);
        _progress.IsVisible = snapshot.State == UpdateState.Downloading;
        _progress.Value = snapshot.Progress is { } progress ? progress.Fraction * 100 : 0;
        _releaseButton.IsVisible = snapshot.Candidate is not null;
        _downloadButton.IsVisible = snapshot.Candidate is not null
            && snapshot.State is UpdateState.Available or UpdateState.Deferred;
        _snoozeButton.IsVisible = snapshot.Candidate is not null
            && snapshot.State == UpdateState.Available;
        _revealButton.IsVisible = snapshot.Download is not null;
        _applyButton.IsVisible = snapshot.Download is not null
            && Coordinator is not null;
        _downloadButton.IsEnabled = snapshot.State is UpdateState.Available or UpdateState.Deferred;
        _checkButton.IsEnabled = snapshot.State != UpdateState.Downloading;

        if (snapshot.ShouldNotify)
            _ = PresentNotificationAsync(snapshot);
    }

    private async Task PresentNotificationAsync(UpdateSnapshot snapshot)
    {
        if (Interlocked.Exchange(ref _notificationInFlight, 1) != 0)
            return;
        try
        {
            if (HostActions is { } host)
            {
                if (NotificationMode == UpdateNotificationMode.Popup)
                    await host.ShowPopupAsync(snapshot).ConfigureAwait(true);
                else if (NotificationMode == UpdateNotificationMode.Toast)
                    await host.ShowToastAsync(snapshot).ConfigureAwait(true);
            }

            if (Coordinator is { } coordinator)
                await coordinator.AcknowledgeNotificationAsync().ConfigureAwait(true);
        }
        finally
        {
            Interlocked.Exchange(ref _notificationInFlight, 0);
        }
    }

    private async Task CheckAsync()
    {
        if (Coordinator is not null)
            await Coordinator.CheckAsync(force: true).ConfigureAwait(true);
    }

    private async Task OpenReleaseAsync()
    {
        if (Coordinator?.Snapshot.Candidate is { } candidate && HostActions is { } host)
            await host.OpenReleaseAsync(candidate.ReleaseUri).ConfigureAwait(true);
    }

    private async Task DownloadAsync()
    {
        if (Coordinator is not null)
            await Coordinator.DownloadAsync().ConfigureAwait(true);
    }

    private async Task SnoozeAsync()
    {
        if (Coordinator is not null)
            await Coordinator.SnoozeAsync().ConfigureAwait(true);
    }

    private async Task RevealAsync()
    {
        if (Coordinator?.Snapshot.Download is { } download && HostActions is { } host)
            await host.RevealDownloadAsync(download.Path).ConfigureAwait(true);
    }

    private async Task ApplyAsync()
    {
        if (Coordinator is not null)
            await Coordinator.ApplyAsync().ConfigureAwait(true);
    }

    private static TextBlock CreateText(string text, string automationId) =>
        SetAutomation(new TextBlock
        {
            Text = text,
        }, text, automationId);

    private static Button CreateButton(string text, string automationId) =>
        SetAutomation(new Button
        {
            Content = text,
            MinWidth = 96,
        }, text, automationId);

    private static T SetAutomation<T>(T control, string name, string automationId)
        where T : Control
    {
        control.SetValue(AutomationProperties.NameProperty, name);
        control.SetValue(AutomationProperties.AutomationIdProperty, automationId);
        return control;
    }
}
