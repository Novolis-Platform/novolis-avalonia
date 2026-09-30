using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Manuscript;

namespace Novolis.Avalonia.Manuscript;

/// <summary>Read-only diagnostics list bound to <see cref="DiagnosticFinding"/> rows.</summary>
public sealed class DiagnosticsListPane : UserControl
{
    readonly ListBox _list = new();

    /// <summary>Creates an empty diagnostics pane.</summary>
    public DiagnosticsListPane()
    {
        Content = _list;
    }

    /// <summary>Rebinds findings.</summary>
    public void SetFindings(IReadOnlyList<DiagnosticFinding> findings)
    {
        _list.ItemsSource = findings
            .Select(f => $"{f.Severity}: [{f.Code}] {f.Message}" + (string.IsNullOrWhiteSpace(f.Path) ? "" : $" ({f.Path})"))
            .ToList();
    }
}
