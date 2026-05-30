using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Novolis.Avalonia.Studio;

/// <summary>Flash messages and busy state for studio toolbar actions.</summary>
public sealed class StudioFeedback
{
    private readonly TextBlock _statusLine;
    private readonly TextBlock _flashLine;
    private readonly Border _busyOverlay;
    private readonly TextBlock _busyText;
    private DispatcherTimer? _clearFlashTimer;

    public StudioFeedback(TextBlock statusLine, TextBlock flashLine, Border busyOverlay, TextBlock busyText)
    {
        _statusLine = statusLine;
        _flashLine = flashLine;
        _busyOverlay = busyOverlay;
        _busyText = busyText;
    }

    public void SetStatus(string text) => RunOnUiThread(() => _statusLine.Text = text);

    public void Flash(string message, TimeSpan? duration = null)
    {
        RunOnUiThread(() =>
        {
            _flashLine.Text = message;
            _flashLine.Foreground = Brushes.LightGreen;
            _clearFlashTimer?.Stop();
            _clearFlashTimer = new DispatcherTimer(duration ?? TimeSpan.FromSeconds(3), DispatcherPriority.Normal, (_, _) =>
            {
                _flashLine.Text = string.Empty;
                _clearFlashTimer?.Stop();
            });
            _clearFlashTimer.Start();
        });
    }

    public void FlashWarning(string message) => Flash(message, TimeSpan.FromSeconds(4));

    public void FlashError(string message)
    {
        RunOnUiThread(() =>
        {
            _flashLine.Text = message;
            _flashLine.Foreground = Brushes.OrangeRed;
            _clearFlashTimer?.Stop();
            _clearFlashTimer = new DispatcherTimer(TimeSpan.FromSeconds(6), DispatcherPriority.Normal, (_, _) =>
            {
                _flashLine.Text = string.Empty;
                _flashLine.Foreground = Brushes.LightGreen;
                _clearFlashTimer?.Stop();
            });
            _clearFlashTimer.Start();
        });
    }

    public void SetBusy(string message)
    {
        RunOnUiThread(() =>
        {
            _busyText.Text = message;
            _busyOverlay.IsVisible = true;
        });
        Flash(message, TimeSpan.FromSeconds(30));
    }

    public void ClearBusy() => RunOnUiThread(() => _busyOverlay.IsVisible = false);

    private static void RunOnUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    public async Task RunAsync(string busyMessage, string successMessage, Func<Task> action)
    {
        SetBusy(busyMessage);
        try
        {
            await action().ConfigureAwait(true);
            ClearBusy();
            Flash(successMessage);
        }
        catch (Exception ex)
        {
            ClearBusy();
            FlashError($"{busyMessage} failed: {ex.Message}");
            throw;
        }
    }

    public void RunSync(string busyMessage, string successMessage, Action action)
    {
        SetBusy(busyMessage);
        try
        {
            action();
            ClearBusy();
            Flash(successMessage);
        }
        catch (Exception ex)
        {
            ClearBusy();
            FlashError($"{busyMessage} failed: {ex.Message}");
            throw;
        }
    }
}
