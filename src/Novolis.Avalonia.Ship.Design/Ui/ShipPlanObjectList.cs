using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Services;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>Semantic hierarchy: Hull / Structure / Deck N / Equipment (§30).</summary>
public static class ShipPlanObjectList
{
    public static Control Build(ShipDesignSession session)
    {
        var tree = new TreeView
        {
            MinHeight = 220,
            Background = new SolidColorBrush(Color.Parse("#14161a")),
        };

        void Refresh()
        {
            var d = session.Design;
            var roots = new List<TreeViewItem>();

            roots.Add(Leaf("Hull", d.Hull.Id.AsObject(), session));

            var structure = new TreeViewItem { Header = "Structure", IsExpanded = true };
            var frames = new TreeViewItem { Header = "Frames", IsExpanded = false };
            foreach (var f in d.Frames)
                frames.Items.Add(Leaf(f.Name, f.Id.AsObject(), session));
            var longs = new TreeViewItem { Header = "Longitudinals", IsExpanded = false };
            foreach (var l in d.Longitudinals)
                longs.Items.Add(Leaf(l.Name, l.Id.AsObject(), session));
            var decksNode = new TreeViewItem { Header = "Decks", IsExpanded = false };
            foreach (var deck in d.Decks)
                decksNode.Items.Add(Leaf(deck.Name, deck.Id.AsObject(), session));
            var bhNode = new TreeViewItem { Header = "Structural Bulkheads", IsExpanded = false };
            foreach (var b in d.Bulkheads.Where(x => x.IsPrimary))
                bhNode.Items.Add(Leaf(b.Name, b.Id.AsObject(), session));
            structure.Items.Add(frames);
            structure.Items.Add(longs);
            structure.Items.Add(decksNode);
            structure.Items.Add(bhNode);
            roots.Add(structure);

            foreach (var deck in d.Decks)
            {
                var deckItem = new TreeViewItem
                {
                    Header = deck.Name,
                    IsExpanded = deck.Index == session.ActiveDeckIndex,
                    Tag = deck.Id.AsObject(),
                };
                foreach (var c in d.Compartments.Where(x => x.DeckId.Value == deck.Id.Value))
                    deckItem.Items.Add(Leaf($"Cmp · {c.Name}", c.Id.AsObject(), session));
                foreach (var p in d.Passages.Where(x => x.DeckId.Value == deck.Id.Value))
                    deckItem.Items.Add(Leaf($"Pass · {p.Name}", p.Id.AsObject(), session));
                foreach (var o in d.Openings)
                {
                    // Openings hosted by bulkheads on this deck, or any opening when deck active.
                    var bh = d.Bulkheads.FirstOrDefault(b => b.Id.Value == o.HostId.Value);
                    if (bh?.DeckId is { } bid && bid.Value == deck.Id.Value)
                        deckItem.Items.Add(Leaf($"Open · {o.Name}", o.Id.AsObject(), session));
                }

                roots.Add(deckItem);
            }

            var equip = new TreeViewItem { Header = "Equipment", IsExpanded = true };
            foreach (var e in d.Equipment)
                equip.Items.Add(Leaf(e.Name, e.Id.AsObject(), session));
            roots.Add(equip);

            tree.ItemsSource = roots;
        }

        session.Changed += Refresh;
        Refresh();

        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(8) };
        panel.Children.Add(new TextBlock
        {
            Text = "Hierarchy",
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.LightGray,
        });
        panel.Children.Add(tree);

        var addPassage = new Button { Content = "Add mid-deck passage", Padding = new Thickness(8, 4) };
        addPassage.Click += (_, _) =>
        {
            if (!session.HasShip || session.Design.Decks.Count == 0)
                return;
            ShipPlanAuthoring.AddDefault(session, ShipDesignTool.Passage);
        };
        session.Changed += () => addPassage.IsEnabled = session.HasShip;
        addPassage.IsEnabled = session.HasShip;
        panel.Children.Add(addPassage);
        return panel;
    }

    private static TreeViewItem Leaf(string label, ShipObjectId id, ShipDesignSession session)
    {
        var item = new TreeViewItem { Header = label, Tag = id };
        item.PointerPressed += (_, _) => session.Select(id);
        return item;
    }
}
