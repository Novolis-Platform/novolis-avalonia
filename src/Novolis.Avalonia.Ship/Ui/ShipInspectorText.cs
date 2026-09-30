using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Session;
using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Avalonia.Ship.Ui;

/// <summary>Formats topology + validation for inspector panes.</summary>
public static class ShipInspectorText
{
    public static string Format(CadSessionService session)
    {
        var doc = session.Document.Document;
        var topo = ShipTopology.Analyze(doc);
        var val = ShipValidator.Validate(doc, topo);
        var lines = new List<string>
        {
            $"Spaces: {topo.SpaceIds.Count} · sealed components: {topo.SealedComponents.Count} · venting: {topo.VentingToExterior.Count}",
            $"Validation: {(val.Ok ? "OK" : "FAIL")} ({val.Issues.Count} issue(s))",
        };
        foreach (var i in val.Issues.Take(12))
            lines.Add($"  [{i.Severity}] {i.Code}: {i.Message}");
        return string.Join('\n', lines);
    }
}
