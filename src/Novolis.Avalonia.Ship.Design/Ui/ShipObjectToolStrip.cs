using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

public static class ShipObjectToolStrip
{
    public static Control Build(ShipDesignSession session, Action<ShipDesignTool>? onToolChanged = null)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(8, 4),
        };

        ShipDesignTool current = ShipDesignTool.Select;
        void Add(string label, ShipDesignTool tool)
        {
            var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
            btn.Click += (_, _) =>
            {
                current = tool;
                onToolChanged?.Invoke(tool);
                if (tool == ShipDesignTool.Hull)
                    session.Select(session.Design.Hull.Id.AsObject());
                else if (tool == ShipDesignTool.Structure && session.Design.Frames.Count > 0)
                    session.Select(session.Design.Frames[0].Id.AsObject());
            };
            row.Children.Add(btn);
        }

        Add("Select", ShipDesignTool.Select);
        Add("Bulkhead", ShipDesignTool.Bulkhead);
        Add("Compartment", ShipDesignTool.Compartment);
        Add("Passage", ShipDesignTool.Passage);
        Add("Opening", ShipDesignTool.Opening);
        Add("Equipment", ShipDesignTool.Equipment);
        Add("Structure", ShipDesignTool.Structure);
        Add("Hull", ShipDesignTool.Hull);

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
            deckBox.Maximum = System.Math.Max(0, session.Design.Decks.Count - 1);
            deckBox.Value = session.ActiveDeckIndex;
        };

        row.Children.Add(new TextBlock
        {
            Text = "Deck",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Foreground = Brushes.Gray,
        });
        row.Children.Add(deckBox);

        _ = current;
        return row;
    }
}
