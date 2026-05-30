using Novolis.Audio.Voice;
using Novolis.Audio.Voice.Platform;

namespace Novolis.Avalonia.Voice;

/// <summary>Creates platform <see cref="IVoiceService"/> instances for studio preview on Windows.</summary>
internal static class PlatformVoicePreviewFactory
{
    public static IVoiceService Create(PlatformSpeechOptions speech, Func<string, string>? normalizeText)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Platform TTS preview requires Windows and Novolis.Audio.Voice.Platform.Windows.");
        }

        var type = Type.GetType(
            "Novolis.Audio.Voice.Platform.Windows.WindowsPlatformVoiceService, Novolis.Audio.Voice.Platform.Windows",
            throwOnError: true)!;
        return (IVoiceService)Activator.CreateInstance(type, speech, normalizeText)!;
    }
}
