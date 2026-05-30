using Avalonia.Threading;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Design;

namespace Novolis.Avalonia.Voice;

/// <summary>Debounced TTS preview with cancellation for voice studio UIs.</summary>
public sealed class VoicePreviewController : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(400);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _speakCts;
    private IVoiceService? _voice;

    public string PreviewPhrase { get; set; } = "Tower, ready for departure.";

    public event Action<string>? StatusChanged;

    public event Action<Exception>? PreviewFailed;

    public void SchedulePreview(VoicePresetDraft draft)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        _ = DebouncePreviewAsync(draft, token);
    }

    public async Task PreviewNowAsync(VoicePresetDraft draft, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RunPreviewAsync(draft, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _gate.Dispose();
    }

    private async Task DebouncePreviewAsync(VoicePresetDraft draft, CancellationToken token)
    {
        try
        {
            await Task.Delay(Debounce, token).ConfigureAwait(false);
            await PreviewNowAsync(draft, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunPreviewAsync(VoicePresetDraft draft, CancellationToken cancellationToken)
    {
        var validation = VoicePresetValidation.Validate(draft);
        if (!validation.IsValid)
        {
            RaiseStatus(string.Join("; ", validation.Errors));
            return;
        }

        _speakCts?.Cancel();
        _speakCts?.Dispose();
        _speakCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            RaiseStatus("Synthesizing…");
            _voice = VoicePresetPreviewFactory.Create(draft);
            await _voice.SpeakAsync(PreviewPhrase, _speakCts.Token).ConfigureAwait(false);
            RaiseStatus("Preview complete.");
        }
        catch (OperationCanceledException)
        {
            RaiseStatus("Preview cancelled.");
        }
        catch (Exception ex)
        {
            RaiseStatus($"Preview failed: {ex.Message}");
            RaisePreviewFailed(ex);
        }
    }

    private void RaiseStatus(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
            StatusChanged?.Invoke(message);
        else
            Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(message));
    }

    private void RaisePreviewFailed(Exception ex)
    {
        if (Dispatcher.UIThread.CheckAccess())
            PreviewFailed?.Invoke(ex);
        else
            Dispatcher.UIThread.Post(() => PreviewFailed?.Invoke(ex));
    }
}
