using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Services;

/// <summary>
/// Optional model-view exterior drawer (e.g. freighter silhouette from <c>Novolis.Avalonia.Cad.Ship</c>).
/// Core CAD stays domain-agnostic; hosts register handlers at startup.
/// </summary>
public static class CadExteriorHooks
{
    /// <summary>When true and isolate is off, <see cref="Draw"/> replaces entity drawing.</summary>
    public static Func<CadDocument, bool>? ShouldUse { get; set; }

    /// <summary>Draw sealed exterior massing for the document.</summary>
    public static Action<CadDocument>? Draw { get; set; }

    /// <summary>Optional HUD lines when exterior mode is active (title, hint).</summary>
    public static Func<CadDocument, (string Title, string Hint)?>? HudLines { get; set; }
}
