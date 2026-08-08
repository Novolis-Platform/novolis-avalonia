using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Small branch/tag pill.</summary>
public sealed class GitRefBadge : Border
{
    readonly TextBlock _text = new() { FontSize = 11, VerticalAlignment = VerticalAlignment.Center };

    /// <summary>Creates an empty badge.</summary>
    public GitRefBadge()
    {
        CornerRadius = new CornerRadius(3);
        Padding = new Thickness(6, 2);
        BorderThickness = new Thickness(1);
        Child = _text;
        SetKind(GitRefKind.Branch);
    }

    /// <summary>Binds tip.</summary>
    public void SetTip(TipRef tip)
    {
        ArgumentNullException.ThrowIfNull(tip);
        _text.Text = tip.Name;
        SetKind(tip.Kind);
    }

    /// <summary>Sets label text.</summary>
    public void SetText(string text) => _text.Text = text;

    void SetKind(GitRefKind kind)
    {
        BorderBrush = new SolidColorBrush(Color.FromRgb(80, 110, 140));
        Background = kind switch
        {
            GitRefKind.Tag => new SolidColorBrush(Color.FromRgb(50, 70, 55)),
            GitRefKind.Remote => new SolidColorBrush(Color.FromRgb(55, 55, 75)),
            _ => new SolidColorBrush(Color.FromRgb(40, 70, 95)),
        };
        _text.Foreground = Brushes.White;
    }
}

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
