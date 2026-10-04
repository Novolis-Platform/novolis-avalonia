using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Services;

/// <summary>
/// Optional model-view exterior drawer (e.g. freighter geometry from
/// <c>Novolis.Avalonia.Cad.Ship</c>).
/// Core CAD stays domain-agnostic; a host supplies handlers for one session.
/// </summary>
public sealed class CadExteriorHooks
{
    /// <summary>When true and isolate is off, <see cref="Draw"/> replaces entity drawing.</summary>
    public Func<CadDocument, bool>? ShouldUse { get; init; }

    /// <summary>Draw sealed exterior massing for the document.</summary>
    public Action<CadDocument>? Draw { get; init; }

    /// <summary>Optional HUD lines when exterior mode is active (title, hint).</summary>
    public Func<CadDocument, (string Title, string Hint)?>? HudLines { get; init; }
}
