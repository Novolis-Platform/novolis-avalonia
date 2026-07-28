namespace Novolis.Avalonia.Controls;

/// <summary>A button option for <see cref="ChoiceDialog"/>.</summary>
/// <param name="Id">Stable id returned when the option is chosen.</param>
/// <param name="Label">Button label.</param>
/// <param name="IsDefault">When true, Enter activates this option.</param>
/// <param name="IsCancel">When true, Escape returns this option's id (otherwise Escape returns null).</param>
public sealed record ChoiceOption(string Id, string Label, bool IsDefault = false, bool IsCancel = false);

/// <summary>Resolves default / cancel option ids without showing UI (unit-testable).</summary>
public static class ChoiceDialogLogic
{
    /// <summary>Returns the first option marked <see cref="ChoiceOption.IsDefault"/>, else the first option.</summary>
    public static ChoiceOption? ResolveDefault(IReadOnlyList<ChoiceOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
            return null;
        return options.FirstOrDefault(o => o.IsDefault) ?? options[0];
    }

    /// <summary>Returns the first option marked <see cref="ChoiceOption.IsCancel"/>, else null.</summary>
    public static ChoiceOption? ResolveCancel(IReadOnlyList<ChoiceOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.FirstOrDefault(o => o.IsCancel);
    }
}
