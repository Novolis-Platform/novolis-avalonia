using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
        var syncing = false;
        var leaves = new Dictionary<Guid, TreeViewItem>();

        void Refresh()
        {
            var d = session.Design;
            var roots = new List<TreeViewItem>();
            leaves.Clear();
            if (!session.HasShip)
            {
                syncing = true;
                try
                {
                    tree.ItemsSource = roots;
                }
                finally
                {
                    syncing = false;
                }

                return;
            }

            roots.Add(Leaf("Hull", d.Hull.Id.AsObject(), session, leaves));

            var structure = new TreeViewItem { Header = "Structure", IsExpanded = true };
            var frames = new TreeViewItem { Header = "Frames", IsExpanded = false };
            foreach (var f in d.Frames)
                frames.Items.Add(Leaf(f.Name, f.Id.AsObject(), session, leaves));
            var longs = new TreeViewItem { Header = "Longitudinals", IsExpanded = false };
            foreach (var l in d.Longitudinals)
                longs.Items.Add(Leaf(l.Name, l.Id.AsObject(), session, leaves));
            var decksNode = new TreeViewItem { Header = "Decks", IsExpanded = false };
            foreach (var deck in d.Decks)
                decksNode.Items.Add(Leaf(deck.Name, deck.Id.AsObject(), session, leaves));
            var bhNode = new TreeViewItem { Header = "Bulkheads", IsExpanded = true };
            foreach (var b in d.Bulkheads)
                bhNode.Items.Add(Leaf(b.Name, b.Id.AsObject(), session, leaves));
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
                    deckItem.Items.Add(Leaf($"Room · {c.Name}", c.Id.AsObject(), session, leaves));
                foreach (var p in d.Passages.Where(x => x.DeckId.Value == deck.Id.Value))
                    deckItem.Items.Add(Leaf($"Pass · {p.Name}", p.Id.AsObject(), session, leaves));
                foreach (var o in d.Openings)
                {
                    var bh = d.Bulkheads.FirstOrDefault(b => b.Id.Value == o.HostId.Value);
                    if (bh?.DeckId is { } bid && bid.Value == deck.Id.Value)
                        deckItem.Items.Add(Leaf($"Open · {o.Name}", o.Id.AsObject(), session, leaves));
                }

                roots.Add(deckItem);
            }

            var equip = new TreeViewItem { Header = "Equipment", IsExpanded = true };
            foreach (var e in d.Equipment)
                equip.Items.Add(Leaf(e.Name, e.Id.AsObject(), session, leaves));
            roots.Add(equip);

            syncing = true;
            try
            {
                tree.ItemsSource = roots;
                if (session.SelectedObjectId is { } sel && leaves.TryGetValue(sel.Value, out var item))
                    tree.SelectedItem = item;
            }
            finally
            {
                syncing = false;
            }
        }

        tree.SelectionChanged += (_, _) =>
        {
            if (syncing)
                return;
            if (tree.SelectedItem is TreeViewItem { Tag: ShipObjectId id })
            {
                session.Select(id);
                session.SetHighlighted([]);
            }
        };

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
        return panel;
    }

    private static TreeViewItem Leaf(
        string label,
        ShipObjectId id,
        ShipDesignSession session,
        Dictionary<Guid, TreeViewItem> leaves)
    {
        var item = new TreeViewItem { Header = label, Tag = id };
        leaves[id.Value] = item;
        item.PointerPressed += (_, _) =>
        {
            session.Select(id);
            session.SetHighlighted([]);
        };
        return item;
    }
}
