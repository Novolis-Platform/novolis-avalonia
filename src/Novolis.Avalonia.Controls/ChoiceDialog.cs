using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Controls;

/// <summary>Modal multi-choice dialog (recovery / conflict / confirm patterns).</summary>
public sealed class ChoiceDialog : Window
{
    readonly TaskCompletionSource<string?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    ChoiceDialog(string title, string message, string? detail, IReadOnlyList<ChoiceOption> options)
    {
        Title = title;
        Width = 480;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var messageBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        };
        var detailBlock = new TextBlock
        {
            Text = detail ?? "",
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = !string.IsNullOrWhiteSpace(detail),
            Margin = new Thickness(0, 8, 0, 0)
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 16, 0, 0)
        };

        foreach (var option in options)
        {
            var btn = new Button
            {
                Content = option.Label,
                Tag = option.Id,
                IsDefault = option.IsDefault,
                IsCancel = option.IsCancel
            };
            btn.Click += (_, _) => Complete(option.Id);
            buttons.Children.Add(btn);
        }

        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Children = { messageBlock, detailBlock, buttons }
        };

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                var cancel = ChoiceDialogLogic.ResolveCancel(options);
                Complete(cancel?.Id);
                e.Handled = true;
            }
        };

        Closed += (_, _) => _tcs.TrySetResult(null);
    }

    void Complete(string? id)
    {
        if (_tcs.TrySetResult(id))
            Close();
    }

    /// <summary>Shows a modal choice dialog and returns the selected option id, or null if dismissed.</summary>
    public static async Task<string?> ShowAsync(
        Window? owner,
        string title,
        string message,
        string? detail,
        IReadOnlyList<ChoiceOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
            throw new ArgumentException("At least one option is required.", nameof(options));

        var dialog = new ChoiceDialog(title, message, detail, options);
        if (owner is not null)
            await dialog.ShowDialog(owner).ConfigureAwait(true);
        else
            dialog.Show();

        return await dialog._tcs.Task.ConfigureAwait(true);
    }
}
