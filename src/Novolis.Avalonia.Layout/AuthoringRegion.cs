namespace Novolis.Avalonia.Layout;

/// <summary>
/// Injectable region of an <see cref="AuthoringWorkspace"/>.
/// </summary>
public enum AuthoringRegion
{
    /// <summary>Left rail (Wide) or first page (Narrow).</summary>
    Nav,

    /// <summary>Center / main content.</summary>
    Primary,

    /// <summary>Right rail (Wide) or optional third page (Narrow).</summary>
    Context,
}
