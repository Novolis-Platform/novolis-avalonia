using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Novolis.Avalonia.Git;

/// <summary>Severity for destructive / caution confirms.</summary>
public enum GitConfirmSeverity
{
    /// <summary>Informational confirm (batch pull, push).</summary>
    Info,

    /// <summary>Caution (pop, dirty checkout).</summary>
    Warning,

    /// <summary>Irreversible / high risk (stash drop, branch-cut apply).</summary>
    Danger,
}

/// <summary>Parameters for <see cref="GitConfirmDialog"/>.</summary>
public sealed class GitConfirmRequest
{
    /// <summary>Window title.</summary>
    public required string Title { get; init; }

    /// <summary>Short summary (one or two sentences).</summary>
    public required string Summary { get; init; }

    /// <summary>Optional mono detail block.</summary>
    public string? Detail { get; init; }

    /// <summary>Risk level.</summary>
    public GitConfirmSeverity Severity { get; init; } = GitConfirmSeverity.Warning;

    /// <summary>Confirm button label.</summary>
    public string ConfirmLabel { get; init; } = "Continue";

    /// <summary>Cancel button label.</summary>
    public string CancelLabel { get; init; } = "Cancel";

    /// <summary>When set, user must type this exact phrase (case-insensitive) to enable Confirm.</summary>
    public string? RequireTypedPhrase { get; init; }

    /// <summary>Hint under the type-to-confirm box.</summary>
    public string? TypedPhraseHint { get; init; }
}

/// <summary>Modal safety confirm for destructive Git chrome actions.</summary>
public static class GitConfirmDialog
{
    static readonly IBrush ShellBg = new ImmutableSolidColorBrush(Color.FromRgb(22, 24, 28));
    static readonly IBrush PaneBg = new ImmutableSolidColorBrush(Color.FromRgb(32, 34, 40));
    static readonly IBrush DangerBg = new ImmutableSolidColorBrush(Color.FromRgb(90, 32, 38));
    static readonly IBrush WarnBg = new ImmutableSolidColorBrush(Color.FromRgb(90, 70, 28));
    static readonly IBrush InfoBg = new ImmutableSolidColorBrush(Color.FromRgb(32, 56, 72));
    static readonly IBrush Muted = new ImmutableSolidColorBrush(Color.FromRgb(160, 168, 178));

    /// <summary>Shows a modal confirm. Returns true only when the user confirms.</summary>
    public static async Task<bool> ShowAsync(Window owner, GitConfirmRequest request)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(request);

        var tcs = new TaskCompletionSource<bool>();
        var confirm = new Button
        {
            Content = request.ConfirmLabel,
            MinWidth = 110,
            IsEnabled = string.IsNullOrEmpty(request.RequireTypedPhrase),
        };
        var cancel = new Button { Content = request.CancelLabel, MinWidth = 90 };

        Window? dlg = null;
        cancel.Click += (_, _) =>
        {
            tcs.TrySetResult(false);
            dlg?.Close();
        };
        confirm.Click += (_, _) =>
        {
            tcs.TrySetResult(true);
            dlg?.Close();
        };

        var badge = request.Severity switch
        {
            GitConfirmSeverity.Danger => ("DESTRUCTIVE", DangerBg),
            GitConfirmSeverity.Warning => ("CAUTION", WarnBg),
            _ => ("CONFIRM", InfoBg),
        };

        var body = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(16),
            Children =
            {
                new Border
                {
                    Background = badge.Item2,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Child = new TextBlock
                    {
                        Text = badge.Item1,
                        FontWeight = FontWeight.Bold,
                        FontSize = 11,
                    },
                },
                new TextBlock
                {
                    Text = request.Summary,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                },
            },
        };

        if (!string.IsNullOrWhiteSpace(request.Detail))
        {
            body.Children.Add(new Border
            {
                Background = PaneBg,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
                Child = new TextBlock
                {
                    Text = request.Detail,
                    FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.95,
                },
            });
        }

        if (!string.IsNullOrEmpty(request.RequireTypedPhrase))
        {
            var phrase = request.RequireTypedPhrase;
            var hint = request.TypedPhraseHint
                       ?? $"Type {phrase} to enable the confirm button.";
            body.Children.Add(new TextBlock
            {
                Text = hint,
                Foreground = Muted,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });
            var box = new TextBox { PlaceholderText = phrase };
            box.TextChanged += (_, _) =>
            {
                confirm.IsEnabled = string.Equals(
                    box.Text?.Trim(),
                    phrase,
                    StringComparison.OrdinalIgnoreCase);
            };
            body.Children.Add(box);
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(12),
            Children = { cancel, confirm },
        };

        dlg = new Window
        {
            Title = request.Title,
            Width = 520,
            Height = string.IsNullOrEmpty(request.RequireTypedPhrase) ? 280 : 340,
            MinWidth = 420,
            MinHeight = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = ShellBg,
            Foreground = Brushes.WhiteSmoke,
            CanResize = true,
            Content = new DockPanel
            {
                LastChildFill = true,
                Children = { buttons, body },
            },
        };
        DockPanel.SetDock(buttons, Dock.Bottom);

        dlg.Closed += (_, _) => tcs.TrySetResult(false);
        await dlg.ShowDialog(owner).ConfigureAwait(true);
        return await tcs.Task.ConfigureAwait(true);
    }
}
