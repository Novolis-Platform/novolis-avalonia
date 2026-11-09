namespace Novolis.Avalonia.Speech;

/// <summary>Capabilities exposed by the selected speech provider.</summary>
public sealed record SpeechCapabilities(
    bool CanDevicePlayback,
    bool CanAzurePlayback,
    bool CanCreateMp3)
{
    /// <summary>Capabilities when only local device speech is selected.</summary>
    public static SpeechCapabilities DeviceOnly { get; } = new(
        CanDevicePlayback: true,
        CanAzurePlayback: false,
        CanCreateMp3: false);

    /// <summary>Capabilities when Edge Read Aloud is selected (MP3 without Azure credentials).</summary>
    public static SpeechCapabilities EdgeTts { get; } = new(
        CanDevicePlayback: true,
        CanAzurePlayback: false,
        CanCreateMp3: true);
}
