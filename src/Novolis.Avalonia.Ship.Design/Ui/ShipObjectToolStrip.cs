using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Services;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

public static class ShipObjectToolStrip
{
    public static Control Build(ShipDesignSession session, Action<string>? onStatus = null)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(8, 4),
        };

        void Add(string label, ShipDesignTool tool, bool placeable)
        {
            var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
            btn.Click += (_, _) =>
            {
                if (!session.HasShip && tool is not ShipDesignTool.Select)
                {
                    onStatus?.Invoke("Create a ship first (Create ship panel → Create ship).");
                    return;
                }

                session.SetActiveTool(tool);
                if (tool == ShipDesignTool.Hull && session.HasShip)
                    session.Select(session.Design.Hull.Id.AsObject());
                else if (tool == ShipDesignTool.Structure && session.Design.Frames.Count > 0)
                    session.Select(session.Design.Frames[0].Id.AsObject());

                if (placeable && session.Workspace == ShipWorkspaceKind.Plan)
                    onStatus?.Invoke(ShipPlanAuthoring.ToolHint(tool) + " · Shift+click tool = place default");
                else
                    onStatus?.Invoke(ShipPlanAuthoring.ToolHint(tool));
            };
            btn.PointerPressed += (_, e) =>
            {
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    return;
                if (!placeable)
                    return;
                e.Handled = true;
                session.SetActiveTool(tool);
                var msg = ShipPlanAuthoring.AddDefault(session, tool);
                if (msg is not null)
                    onStatus?.Invoke(msg);
            };
            row.Children.Add(btn);
        }

        Add("Select", ShipDesignTool.Select, placeable: false);
        Add("Bulkhead", ShipDesignTool.Bulkhead, placeable: true);
        Add("Compartment", ShipDesignTool.Compartment, placeable: true);
        Add("Passage", ShipDesignTool.Passage, placeable: true);
        Add("Opening", ShipDesignTool.Opening, placeable: true);
        Add("Equipment", ShipDesignTool.Equipment, placeable: true);
        Add("Structure", ShipDesignTool.Structure, placeable: false);
        Add("Hull", ShipDesignTool.Hull, placeable: false);

        var placeDefault = new Button { Content = "Place default", Padding = new Thickness(10, 4) };
        placeDefault.Click += (_, _) =>
        {
            var msg = ShipPlanAuthoring.AddDefault(session, session.ActiveTool);
            onStatus?.Invoke(msg ?? "Select a create tool first (Bulkhead / Compartment / Passage / Opening / Equipment).");
        };
        row.Children.Add(placeDefault);

        var deckBox = new NumericUpDown
        {
            Width = 72,
            Minimum = 0,
            Maximum = 20,
            Value = session.ActiveDeckIndex,
            FormatString = "0",
        };
        deckBox.ValueChanged += (_, _) =>
        {
            if (deckBox.Value is { } v)
                session.SetActiveDeck((int)v);
        };
        session.Changed += () =>
        {
            deckBox.IsEnabled = session.HasShip;
            deckBox.Maximum = System.Math.Max(0, session.Design.Decks.Count - 1);
            deckBox.Value = session.ActiveDeckIndex;
            placeDefault.IsEnabled = session.HasShip;
        };

        row.Children.Add(new TextBlock
        {
            Text = "Deck",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Foreground = Brushes.Gray,
        });
        row.Children.Add(deckBox);
        return row;
    }
}
