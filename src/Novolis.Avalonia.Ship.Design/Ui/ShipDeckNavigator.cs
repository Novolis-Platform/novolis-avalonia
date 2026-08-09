using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design.Ui;

/// <summary>Ladder over <see cref="ShipDesign.Decks"/> for PLAN deck navigation.</summary>
public static class ShipDeckNavigator
{
    public static Control Build(ShipDesignSession session)
    {
        var list = new StackPanel { Spacing = 2, Margin = new Thickness(8, 0) };
        var title = new TextBlock
        {
            Text = "Decks",
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 4),
        };
        var root = new StackPanel { Spacing = 4 };
        root.Children.Add(title);
        root.Children.Add(list);

        void Rebuild()
        {
            list.Children.Clear();
            for (var i = session.Design.Decks.Count - 1; i >= 0; i--)
            {
                var deck = session.Design.Decks[i];
                var index = i;
                var active = index == session.ActiveDeckIndex;
                var btn = new Button
                {
                    Content = $"{deck.Name}  ({ShipLengths.ToMeters(deck.Elevation):0.##} m)",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(8, 4),
                    Background = active
                        ? new SolidColorBrush(Color.Parse("#2a4a5a"))
                        : Brushes.Transparent,
                    Foreground = Brushes.WhiteSmoke,
                };
                btn.Click += (_, _) =>
                {
                    session.SetActiveDeck(index);
                    session.Select(deck.Id.AsObject());
                };
                list.Children.Add(btn);
            }
        }

        session.Changed += Rebuild;
        Rebuild();
        return root;
    }
}
