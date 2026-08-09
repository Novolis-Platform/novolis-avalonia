using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Session;
using Novolis.Ship.Topology;
using Novolis.Ship.Validation;

namespace Novolis.Avalonia.Ship.Ui;

internal static class ShipToolStrip
{
    public static Control Build(
        CadSessionService session,
        Action<int>? onDeckChanged,
        Func<int>? getDeck)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
        };

        void AddAction(string label, string actionId)
        {
            var btn = new Button { Content = label, Padding = new global::Avalonia.Thickness(10, 4) };
            btn.Click += (_, _) => session.Execute(new CadCommandDto { ActionId = actionId });
            row.Children.Add(btn);
        }

        AddAction("Validate Ship", ShipChrome.ValidateShipActionId);
        AddAction("Airtight", ShipChrome.RefreshAirtightActionId);
        AddAction("Place Hatch", ShipChrome.PlaceHatchActionId);

        var deckBox = new NumericUpDown
        {
            Width = 72,
            Minimum = -5,
            Maximum = 5,
            Value = getDeck?.Invoke() ?? 0,
            FormatString = "0",
        };
        deckBox.ValueChanged += (_, _) =>
        {
            if (deckBox.Value is { } v)
                onDeckChanged?.Invoke((int)v);
        };
        row.Children.Add(new TextBlock
        {
            Text = "Deck",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Gray,
        });
        row.Children.Add(deckBox);

        return row;
    }
}

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
