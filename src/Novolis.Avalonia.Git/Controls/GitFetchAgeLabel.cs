using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Shows last-fetch age.</summary>
public sealed class GitFetchAgeLabel : TextBlock
{
    /// <summary>Creates the label.</summary>
    public GitFetchAgeLabel()
    {
        FontSize = 11;
        Opacity = 0.75;
        Text = "fetch: —";
    }

    /// <summary>Updates from timestamp.</summary>
    public void SetLastFetch(DateTimeOffset? when)
    {
        if (when is null)
        {
            Text = "fetch: never";
            return;
        }

        var age = DateTimeOffset.UtcNow - when.Value;
        Text = age.TotalMinutes < 1
            ? "fetch: just now"
            : age.TotalHours < 1
                ? $"fetch: {(int)age.TotalMinutes}m ago"
                : $"fetch: {(int)age.TotalHours}h ago";
    }
}
