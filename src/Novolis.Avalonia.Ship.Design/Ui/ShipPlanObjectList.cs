using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>PLAN object tree for selection (structure overlays).</summary>
public static class ShipPlanObjectList
{
    public static Control Build(ShipDesignSession session)
    {
        var list = new ListBox
        {
            MinHeight = 180,
            Background = new SolidColorBrush(Color.Parse("#14161a")),
        };

        void Refresh()
        {
            var items = new List<PlanItem>();
            var d = session.Design;
            items.Add(new PlanItem("Hull", d.Hull.Id.AsObject()));
            foreach (var deck in d.Decks)
                items.Add(new PlanItem($"Deck · {deck.Name}", deck.Id.AsObject()));
            foreach (var f in d.Frames)
                items.Add(new PlanItem($"Frame · {f.Name}", f.Id.AsObject()));
            foreach (var l in d.Longitudinals)
                items.Add(new PlanItem($"Long · {l.Name}", l.Id.AsObject()));
            foreach (var b in d.Bulkheads)
                items.Add(new PlanItem($"BH · {b.Name}", b.Id.AsObject()));
            foreach (var c in d.Compartments)
                items.Add(new PlanItem($"Cmp · {c.Name}", c.Id.AsObject()));
            foreach (var p in d.Passages)
                items.Add(new PlanItem($"Pass · {p.Name}", p.Id.AsObject()));
            foreach (var o in d.Openings)
                items.Add(new PlanItem($"Open · {o.Name}", o.Id.AsObject()));
            foreach (var e in d.Equipment)
                items.Add(new PlanItem($"Eq · {e.Name}", e.Id.AsObject()));
            list.ItemsSource = items;
        }

        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is PlanItem item)
                session.Select(item.Id);
        };

        session.Changed += Refresh;
        Refresh();

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(8) };
        panel.Children.Add(new TextBlock
        {
            Text = "Objects",
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.LightGray,
        });
        panel.Children.Add(list);

        var gripHint = new TextBlock
        {
            Text = "PLAN: select objects · grips edit station/elevation/path on the same semantic object.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = Brushes.Gray,
        };
        panel.Children.Add(gripHint);

        var addPassage = new Button { Content = "Add mid-deck passage", Padding = new Thickness(8, 4) };
        addPassage.Click += (_, _) =>
        {
            if (session.Design.Decks.Count == 0)
                return;
            var deck = session.Design.Decks[System.Math.Clamp(session.ActiveDeckIndex, 0, session.Design.Decks.Count - 1)];
            var L = session.Design.Ship.LengthMeters;
            session.Mutate(d => ShipDesignMutations.AddPassage(
                d,
                deck.Id,
                "Corridor",
                [[0f, -L * 0.35f], [0f, L * 0.35f]],
                widthM: 1.2f,
                heightM: 2.2f));
        };
        panel.Children.Add(addPassage);

        return panel;
    }

    private sealed record PlanItem(string Label, ShipObjectId Id)
    {
        public override string ToString() => Label;
    }
}
