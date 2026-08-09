using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Plan;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

public static class ShipObjectToolStrip
{
    public static Control Build(
        ShipDesignSession session,
        ShipArchitectToolController tools,
        Action<string>? onStatus = null)
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

        void Add(string label, ShipDesignTool tool)
        {
            var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
            toolButtons[tool] = btn;
            btn.Click += (_, _) =>
            {
                if (!session.HasShip && tool is not ShipDesignTool.Select)
                {
                    onStatus?.Invoke("Create a ship first.");
                    return;
                }

                session.SetActiveTool(tool);
                tools.OnToolChanged();
                if (tool == ShipDesignTool.Hull && session.HasShip)
                    session.Select(session.Design.Hull.Id.AsObject());
                else if (tool == ShipDesignTool.Structure && session.Design.Frames.Count > 0)
                    session.Select(session.Design.Frames[0].Id.AsObject());

                var hint = ShipArchitectToolController.Hint(tool);
                session.SetStatusMessage(hint);
                onStatus?.Invoke(hint);
                RefreshHighlight();
            };
            row.Children.Add(btn);
        }

        Add("Select", ShipDesignTool.Select);
        Add("Wall", ShipDesignTool.Bulkhead);
        Add("Room", ShipDesignTool.Compartment);
        Add("Passage", ShipDesignTool.Passage);
        Add("Opening", ShipDesignTool.Opening);
        Add("Equipment", ShipDesignTool.Equipment);
        Add("Structure", ShipDesignTool.Structure);
        Add("Hull", ShipDesignTool.Hull);

        var undo = new Button { Content = "Undo", Padding = new Thickness(10, 4) };
        undo.Click += (_, _) =>
        {
            if (session.TryUndo())
                onStatus?.Invoke("Undo");
        };
        row.Children.Add(undo);

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
            undo.IsEnabled = session.HasShip;
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
