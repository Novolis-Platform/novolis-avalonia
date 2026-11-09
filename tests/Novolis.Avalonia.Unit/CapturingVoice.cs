using Novolis.Audio.Voice;

namespace Novolis.Avalonia.Unit;

sealed class CapturingVoice : IVoiceService
{
    public string? SpokenText { get; private set; }

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        SpokenText = text;
        return Task.CompletedTask;
    }

    public Task WriteToFileAsync(
        string text,
        FileInfo destination,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
