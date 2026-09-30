using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Shared chrome list styling so rows stay readable on dark shells.</summary>
internal static class GitChromeUi
{
    public static void BindTextList<T>(ListBox list, Func<T, string> text)
    {
        list.ItemTemplate = new FuncDataTemplate<T>((item, _) =>
            new TextBlock
            {
                Text = text(item),
                Foreground = Brushes.WhiteSmoke,
                Margin = new Thickness(6, 2),
                TextWrapping = TextWrapping.NoWrap,
            },
            supportsRecycling: true);
    }
}
