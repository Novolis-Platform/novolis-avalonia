namespace Novolis.Avalonia.Speech;

/// <summary>Voice row for <see cref="SpeechVoicePicker"/>.</summary>
public sealed record SpeechVoiceChoice(
    string Id,
    string DisplayName,
    string Locale)
{
    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
