namespace Novolis.Avalonia.Speech;

/// <summary>Raised when an operation is unavailable for the selected provider.</summary>
public sealed class SpeechCapabilityException : InvalidOperationException
{
    /// <summary>Creates an exception with a user-facing explanation.</summary>
    public SpeechCapabilityException(string message)
        : base(message)
    {
    }
}
