namespace Novolis.Avalonia.Controls;

/// <summary>Filter helpers for <see cref="FilteredPickerDialog{T}"/> (unit-testable).</summary>
public static class FilteredPickerLogic
{
    /// <summary>Filters <paramref name="items"/> with <paramref name="predicate"/> against <paramref name="query"/>.</summary>
    public static IReadOnlyList<T> Filter<T>(
        IEnumerable<T> items,
        string? query,
        Func<T, string, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(predicate);
        var q = query ?? "";
        return items.Where(item => predicate(item, q)).ToList();
    }

    /// <summary>Default case-insensitive contains filter using <paramref name="display"/>.</summary>
    public static Func<T, string, bool> ContainsDisplay<T>(Func<T, string> display) =>
        (item, query) =>
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;
            return display(item).Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
        };
}
