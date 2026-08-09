using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>Contextual numeric properties bound to the selected <see cref="ShipObjectId"/> (§20).</summary>
public static class ShipPropertyEditor
{
    public static Control Build(ShipDesignSession session)
    {
        var panel = new StackPanel { Spacing = 6, Margin = new Thickness(8) };
        var title = new TextBlock
        {
            Text = "Properties",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
        };
        var host = new StackPanel { Spacing = 6 };
        panel.Children.Add(title);
        panel.Children.Add(host);

        void Rebuild()
        {
            host.Children.Clear();
            var id = session.SelectedObjectId;
            if (id is null)
            {
                host.Children.Add(Hint("(select an object)"));
                return;
            }

            var d = session.Design;
            var guid = id.Value.Value;

            var deck = d.Decks.FirstOrDefault(x => x.Id.Value == guid);
            if (deck is not null)
            {
                host.Children.Add(Hint($"Deck · {deck.Name}"));
                host.Children.Add(NumRow(
                    "Elevation (m)",
                    ShipLengths.ToMeters(deck.Elevation),
                    v => session.Mutate(des => ShipDesignMutations.SetDeckElevation(des, deck.Id, v))));
                return;
            }

            var frame = d.Frames.FirstOrDefault(x => x.Id.Value == guid);
            if (frame is not null)
            {
                host.Children.Add(Hint($"Frame · {frame.Name}"));
                host.Children.Add(NumRow(
                    "Station (m)",
                    ShipLengths.ToMeters(frame.Station),
                    v => session.Mutate(des => ShipDesignMutations.SetFrameStation(des, frame.Id, v))));
                return;
            }

            var bh = d.Bulkheads.FirstOrDefault(x => x.Id.Value == guid);
            if (bh is not null)
            {
                host.Children.Add(Hint($"Bulkhead · {bh.Name}"));
                host.Children.Add(NumRow(
                    "Thickness (m)",
                    ShipLengths.ToMeters(bh.Thickness),
                    v => session.Mutate(des => ShipDesignMutations.SetBulkheadThickness(des, bh.Id, v)),
                    min: 0.02m,
                    max: 1m,
                    format: "0.###"));
                return;
            }

            var passage = d.Passages.FirstOrDefault(x => x.Id.Value == guid);
            if (passage is not null)
            {
                host.Children.Add(Hint($"Passage · {passage.Name}"));
                host.Children.Add(NumRow(
                    "Width (m)",
                    ShipLengths.ToMeters(passage.Width),
                    v => session.Mutate(des => ShipDesignMutations.SetPassageWidth(des, passage.Id, v)),
                    min: 0.6m,
                    max: 8m,
                    format: "0.##"));
                return;
            }

            if (d.Hull.Id.Value == guid)
            {
                host.Children.Add(Hint("Hull"));
                host.Children.Add(Hint($"Generator: {d.Hull.Generator}"));
                host.Children.Add(Hint($"Thickness: {ShipLengths.ToMeters(d.Hull.Thickness):0.###} m"));
                return;
            }

            host.Children.Add(Hint($"Object {guid:N}"));
            host.Children.Add(Hint("Use MODEL for CAD construction edits."));
        }

        session.Changed += Rebuild;
        Rebuild();
        return panel;
    }

    private static Control NumRow(
        string label,
        float value,
        Action<float> apply,
        decimal min = -200m,
        decimal max = 200m,
        string format = "0.##")
    {
        var box = new NumericUpDown
        {
            Value = (decimal)value,
            Minimum = min,
            Maximum = max,
            Increment = 0.1m,
            FormatString = format,
            Width = 120,
        };
        box.ValueChanged += (_, _) =>
        {
            if (box.Value is { } v)
                apply((float)v);
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 110,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.LightGray,
        });
        row.Children.Add(box);
        return row;
    }

    private static TextBlock Hint(string text) => new()
    {
        Text = text,
        Foreground = Brushes.Gray,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
    };
}
