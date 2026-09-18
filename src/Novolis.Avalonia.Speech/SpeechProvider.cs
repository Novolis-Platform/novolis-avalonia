namespace Novolis.Avalonia.Speech;

/// <summary>Speech source selected by the application.</summary>
public enum SpeechProvider
{
    /// <summary>Use the device's local voice engine without a network request.</summary>
    DeviceVoice,

    /// <summary>Use the user's Azure Speech resource.</summary>
    AzureSpeech,
}
