using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Studio;

/// <summary>Payload for <see cref="StudioCommandBar.Submitted"/>.</summary>
public sealed class StudioCommandSubmittedEventArgs : EventArgs
{
    /// <summary>Creates event args with the submitted prompt text.</summary>
    public StudioCommandSubmittedEventArgs(string text) => Text = text;

    /// <summary>Trimmed prompt text.</summary>
    public string Text { get; }
}
