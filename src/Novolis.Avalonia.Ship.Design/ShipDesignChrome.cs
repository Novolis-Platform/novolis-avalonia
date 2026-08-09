using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Ship;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Avalonia.Ship.Design.Ui;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design;

/// <summary>Attaches object-first ship design chrome to a Cad session + design session.</summary>
public static class ShipDesignChrome
{
    /// <summary>Cad.Ship exterior/import + legacy validate actions + design session wiring.</summary>
    public static void Attach(CadSessionService cad, ShipDesignSession design)
    {
        ArgumentNullException.ThrowIfNull(cad);
        ArgumentNullException.ThrowIfNull(design);
        ShipChrome.Attach(cad);

        // Keep Cad document as a projected mirror for MODEL tools / exterior preview.
        void SyncCad()
        {
            var flat = ShipCadProjector.ToCadDocument(design.Design);
            cad.Document.Document.Entities.Clear();
            cad.Document.Document.Entities.AddRange(flat.Entities);
            cad.Document.Document.Name = flat.Name;
            cad.Document.Document.Properties = flat.Properties;
            cad.Document.Notify();
        }

        design.Changed += SyncCad;
        SyncCad();
    }

    /// <summary>PLAN tool strip + workspace bar + create/object panels around a CAD editor surface.</summary>
    public static Control CreateShell(
        CadSessionService cad,
        ShipDesignSession design,
        Control editorSurface,
        TextBlock? status = null,
        TextBlock? inspector = null)
    {
        ArgumentNullException.ThrowIfNull(cad);
        ArgumentNullException.ThrowIfNull(design);
        ArgumentNullException.ThrowIfNull(editorSurface);

        inspector ??= new TextBlock
        {
            FontFamily = new FontFamily("Consolas,Courier New,monospace"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8),
        };
        status ??= new TextBlock { Text = "PLAN", Margin = new Thickness(8, 4), Foreground = Brushes.LightGray };

        void RefreshText()
        {
            inspector.Text = ShipDesignInspector.Format(design);
            status.Text = $"{design.Workspace} · {design.Design.Ship.Name} · deck {design.ActiveDeckIndex}";
        }

        design.Changed += RefreshText;
        RefreshText();

        var tools = ShipObjectToolStrip.Build(design);
        var workspaceBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
        };
        workspaceBar.Children.Add(Ws("PLAN", ShipWorkspaceKind.Plan, design, status));
        workspaceBar.Children.Add(Ws("MODEL", ShipWorkspaceKind.Model, design, status));
        workspaceBar.Children.Add(Ws("PRESENT", ShipWorkspaceKind.Present, design, status));

        var create = ShipCreatePanel.Build(design, RefreshText);
        var objects = ShipPlanObjectList.Build(design);

        var rightStack = new StackPanel { Spacing = 8 };
        rightStack.Children.Add(create);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(objects);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(inspector);

        var right = new ScrollViewer
        {
            Width = 340,
            Content = rightStack,
            Background = new SolidColorBrush(Color.Parse("#1a1c20")),
        };

        var legacyStrip = ShipChrome.CreateToolStrip(
            cad,
            deck => design.SetActiveDeck(deck),
            () => design.ActiveDeckIndex);

        var body = new DockPanel();
        DockPanel.SetDock(tools, Dock.Top);
        DockPanel.SetDock(workspaceBar, Dock.Top);
        DockPanel.SetDock(legacyStrip, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        DockPanel.SetDock(right, Dock.Right);
        body.Children.Add(tools);
        body.Children.Add(workspaceBar);
        body.Children.Add(legacyStrip);
        body.Children.Add(status);
        body.Children.Add(right);
        body.Children.Add(editorSurface);
        return body;
    }

    private static Button Ws(string label, ShipWorkspaceKind kind, ShipDesignSession design, TextBlock status)
    {
        var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
        btn.Click += (_, _) =>
        {
            design.SetWorkspace(kind);
            status.Text = kind switch
            {
                ShipWorkspaceKind.Plan => "PLAN — architecture & structure",
                ShipWorkspaceKind.Model => "MODEL — CAD / mesh on selected object",
                ShipWorkspaceKind.Present => "PRESENT — materials, lights, scene",
                _ => label,
            };
        };
        return btn;
    }
}
