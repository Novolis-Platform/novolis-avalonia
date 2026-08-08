namespace Novolis.Avalonia.Layout;

/// <summary>
/// Visual arrangement for <see cref="AuthoringWorkspace"/>.
/// </summary>
public enum AuthoringLayoutMode
{
    /// <summary>Three columns: nav | primary | context.</summary>
    Wide,

    /// <summary>Single page host; cycle regions with <see cref="AuthoringWorkspace.ShowRegion"/>.</summary>
    Narrow,
}
