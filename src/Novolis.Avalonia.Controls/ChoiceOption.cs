namespace Novolis.Avalonia.Controls;

/// <summary>A button option for <see cref="ChoiceDialog"/>.</summary>
/// <param name="Id">Stable id returned when the option is chosen.</param>
/// <param name="Label">Button label.</param>
/// <param name="IsDefault">When true, Enter activates this option.</param>
/// <param name="IsCancel">When true, Escape returns this option's id (otherwise Escape returns null).</param>
public sealed record ChoiceOption(string Id, string Label, bool IsDefault = false, bool IsCancel = false);
