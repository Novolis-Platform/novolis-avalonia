namespace Novolis.Avalonia.Speech;

/// <summary>Capabilities exposed by the selected speech provider.</summary>
public sealed record SpeechCapabilities(
    bool CanDevicePlayback,
    bool CanAzurePlayback,
    bool CanCreateMp3)
{
    /// <summary>Capabilities when no Azure resource has been configured.</summary>
    public static SpeechCapabilities DeviceOnly { get; } = new(
        CanDevicePlayback: true,
        CanAzurePlayback: false,
        CanCreateMp3: false);
}
