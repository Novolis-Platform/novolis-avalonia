using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Profile-bound diagnostics list for validation and compiled IR results.
/// </summary>
public sealed class AssetStudioDiagnosticsPanel : AssetStudioPanel
{
    private readonly ListBox _items;
    private readonly TextBlock _emptyState;
    private IReadOnlyList<AssetStudioDiagnosticRow> _diagnostics = [];

    public AssetStudioDiagnosticsPanel()
        : base(
            "Diagnostics",
            CreateContent(out var list, out var emptyState),
            "assetstudio.panel.diagnostics")
    {
        _items = list;
        _emptyState = emptyState;
    }

    /// <summary>List control used by accessible diagnostics workflows.</summary>
    public ListBox ItemsControl => _items;

    /// <summary>Current diagnostics in deterministic display order.</summary>
    public IReadOnlyList<AssetStudioDiagnosticRow> Diagnostics => _diagnostics;

    /// <summary>Replaces diagnostics from recipe validation.</summary>
    public void SetDiagnostics(IEnumerable<VisualDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        _diagnostics = diagnostics
            .Where(diagnostic => diagnostic is not null)
            .OrderBy(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.NodeId?.ToString(), StringComparer.Ordinal)
            .Select(diagnostic => new AssetStudioDiagnosticRow(
                diagnostic.Severity,
                diagnostic.Code,
                diagnostic.Message,
                diagnostic.NodeId,
                diagnostic.ConnectionId))
            .ToArray();
        _items.ItemsSource = _diagnostics;
        _items.SelectedIndex = _diagnostics.Count == 0 ? -1 : 0;
        _emptyState.IsVisible = _diagnostics.Count == 0;
        _emptyState.Text = _diagnostics.Count == 0
            ? "✓ No diagnostics · recipe is valid."
            : string.Empty;
    }

    /// <summary>Replaces diagnostics from compiled visual IR.</summary>
    public void SetDiagnostics(VisualIR ir)
    {
        ArgumentNullException.ThrowIfNull(ir);
        SetDiagnostics(ir.Diagnostics);
    }

    private static Control CreateContent(
        out ListBox list,
        out TextBlock emptyState)
    {
        list = new ListBox
        {
            SelectionMode = SelectionMode.Single,
            MinHeight = 120,
        };
        list.SetValue(
            AutomationProperties.NameProperty,
            "Asset Studio diagnostics");
        list.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.diagnostics.items");
        list.ItemTemplate = new FuncDataTemplate<AssetStudioDiagnosticRow>(
            (diagnostic, _) =>
            {
                var row = new TextBlock
                {
                    Text = diagnostic.DisplayText,
                    Margin = new Thickness(6, 4),
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = diagnostic.Severity == VisualDiagnosticSeverity.Error
                        ? FontWeight.SemiBold
                        : FontWeight.Normal,
                };
                row.SetValue(
                    AutomationProperties.NameProperty,
                    diagnostic.DisplayText);
                ProfileBinding.Bind(
                    row,
                    TextBlock.ForegroundProperty,
                    Profile.TextResourceKey);
                return row;
            },
            supportsRecycling: true);

        emptyState = new TextBlock
        {
            Text = "No diagnostics.",
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8),
        };
        ProfileBinding.Bind(
            emptyState,
            TextBlock.ForegroundProperty,
            Profile.AccentResourceKey);

        var content = new Grid();
        content.Children.Add(
            new ScrollViewer
            {
                Content = list,
                HorizontalScrollBarVisibility =
                    global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            });
        content.Children.Add(emptyState);
        return content;
    }
}
