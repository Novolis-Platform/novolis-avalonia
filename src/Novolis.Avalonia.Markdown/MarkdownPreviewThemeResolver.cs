using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Novolis.Avalonia.Markdown;

/// <summary>Resolves preview theme from the operating-system / application theme.</summary>
public static class MarkdownPreviewThemeResolver
{
    /// <summary>Light → <see cref="MarkdownPreviewTheme.GitHubLight"/>; dark → StudioDark.</summary>
    public static MarkdownPreviewTheme FromApplication()
    {
        var variant = Application.Current?.ActualThemeVariant ?? ThemeVariant.Default;
        if (variant == ThemeVariant.Light)
            return MarkdownPreviewTheme.GitHubLight;
        if (variant == ThemeVariant.Dark)
            return MarkdownPreviewTheme.StudioDark;

        try
        {
            var platform = Application.Current?.PlatformSettings;
            if (platform is not null
                && platform.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark)
                return MarkdownPreviewTheme.StudioDark;
        }
        catch
        {
            // Headless hosts.
        }

        return MarkdownPreviewTheme.GitHubLight;
    }
}
