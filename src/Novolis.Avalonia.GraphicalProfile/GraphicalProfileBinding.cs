using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace Novolis.Avalonia.GraphicalProfile;

/// <summary>
/// Binds a control property to an <c>Ngp.*</c> dynamic resource so chrome
/// follows the operating-system theme.
/// </summary>
public static class GraphicalProfileBinding
{
    /// <summary>
    /// Binds <paramref name="property"/> on <paramref name="target"/> to
    /// <paramref name="resourceKey"/>. Brush-typed properties automatically
    /// use the matching <c>Ngp.*Brush</c> resource.
    /// </summary>
    public static void Bind(
        AvaloniaObject target,
        AvaloniaProperty property,
        string resourceKey)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        target[!property] = new DynamicResourceExtension(
            ResolveKey(property, resourceKey));
    }

    /// <summary>Resolves a color key to the brush resource when required.</summary>
    public static string ResolveKey(AvaloniaProperty property, string resourceKey)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        if (resourceKey.EndsWith("Brush", StringComparison.Ordinal))
        {
            return resourceKey;
        }

        return typeof(IBrush).IsAssignableFrom(property.PropertyType)
            ? resourceKey + "Brush"
            : resourceKey;
    }
}
