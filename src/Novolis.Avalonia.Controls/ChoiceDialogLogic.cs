namespace Novolis.Avalonia.Controls;

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
