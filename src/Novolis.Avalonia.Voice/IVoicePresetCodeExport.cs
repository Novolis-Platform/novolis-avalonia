using Avalonia.Controls;
using Novolis.Audio.Voice.Design;

namespace Novolis.Avalonia.Voice;

/// <summary>Host-provided C# export panel bound to a <see cref="VoicePresetDraft"/>.</summary>
public interface IVoicePresetCodeExport
{
    /// <summary>UI control added to the studio layout.</summary>
    Control View { get; }

    /// <summary>Refreshes generated code for the draft.</summary>
    void Bind(VoicePresetDraft? draft);
}
