using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Display-ready diagnostic row with semantic source references preserved.
/// </summary>
public sealed record AssetStudioDiagnosticRow(
    VisualDiagnosticSeverity Severity,
    string Code,
    string Message,
    NodeId? NodeId = null,
    ConnectionId? ConnectionId = null)
{
    /// <summary>Stable accessible text for the diagnostic row.</summary>
    public string DisplayText
    {
        get
        {
            var location = NodeId is { } node
                ? $" · node {node}"
                : ConnectionId is { } connection
                    ? $" · connection {connection}"
                    : string.Empty;
            return $"{Severity} · {Code} · {Message}{location}";
        }
    }
}
