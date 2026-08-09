using Avalonia;
using Avalonia.Controls;
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

        var toolButtons = new Dictionary<ShipDesignTool, Button>();

        void RefreshHighlight()
        {
            foreach (var (tool, btn) in toolButtons)
            {
                var on = session.ActiveTool == tool;
                if (on)
                {
                    btn.Background = new SolidColorBrush(Color.Parse("#2a6f8f"));
                    btn.Foreground = Brushes.White;
                }
                else
                {
                    btn.ClearValue(Button.BackgroundProperty);
                    btn.ClearValue(Button.ForegroundProperty);
                }
            }
        }

        void Add(string label, ShipDesignTool tool, bool placeable)
        {
            var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
            toolButtons[tool] = btn;
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

                // Immediate place so one toolbar click always produces visible geometry.
                if (placeable && session.HasShip && session.Workspace == ShipWorkspaceKind.Plan)
                {
                    var msg = ShipPlanAuthoring.AddDefault(session, tool);
                    onStatus?.Invoke(
                        (msg ?? "Placed.") + " · click plan for another (2 clicks for path/box tools)");
                }
                else
                {
                    onStatus?.Invoke(ShipPlanAuthoring.ToolHint(tool));
                }

                RefreshHighlight();
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
            RefreshHighlight();
        };

        row.Children.Add(new TextBlock
        {
            Text = "Deck",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Foreground = Brushes.Gray,
        });
        row.Children.Add(deckBox);
        RefreshHighlight();
        return row;
    }
}
