using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Ship;
using Novolis.Avalonia.Ship.Design.Grips;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Avalonia.Ship.Design.Services;
using Novolis.Avalonia.Ship.Design.Ui;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design;

/// <summary>Attaches object-first ship design chrome to a Cad session + design session.</summary>
public static class ShipDesignChrome
{
    public static void Attach(CadSessionService cad, ShipDesignSession design)
    {
        ArgumentNullException.ThrowIfNull(cad);
        ArgumentNullException.ThrowIfNull(design);
        ShipChrome.Attach(cad);

        void SyncCad()
        {
            var source = design.Workspace == ShipWorkspaceKind.Model
                ? design.SelectedObjectGeometry() ?? ShipCadProjector.ToCadDocument(design.Design)
                : ShipCadProjector.ToCadDocument(design.Design);

            cad.Document.Document.Entities.Clear();
            cad.Document.Document.Entities.AddRange(source.Entities);
            cad.Document.Document.Name = source.Name;
            cad.Document.Document.Properties = source.Properties;
            cad.Document.Notify();
        }

        design.Changed += SyncCad;
        SyncCad();
    }

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

        var analysisPanel = ShipAnalysisPanel.Build(design);
        analysisPanel.IsVisible = false;

        void RefreshText()
        {
            inspector.Text = ShipDesignInspector.Format(design);
            var val = design.Validation;
            var gripCount = ShipGripCatalog.ForSelection(design.Design, design.SelectedObjectId).Count;
            status.Text =
                $"{design.Workspace} · {design.Design.Ship.Name} · deck {design.ActiveDeckIndex}"
                + $" · val {(val.Ok ? "OK" : "FAIL")}({val.Issues.Count})"
                + $" · analysis {design.Analysis.Worst}"
                + $" · grips {gripCount}";
            analysisPanel.IsVisible = design.Workspace == ShipWorkspaceKind.Analyze;
        }

        design.Changed += RefreshText;
        RefreshText();

        var tools = ShipObjectToolStrip.Build(design);
        var snapRow = ShipSnapSettings.Build(design);
        var analysisStrip = ShipAnalysisStatusStrip.Build(design);
        var workspaceBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
        };
        workspaceBar.Children.Add(Ws("PLAN", ShipWorkspaceKind.Plan, design, status));
        workspaceBar.Children.Add(Ws("MODEL", ShipWorkspaceKind.Model, design, status));
        workspaceBar.Children.Add(Ws("ANALYZE", ShipWorkspaceKind.Analyze, design, status, () =>
        {
            status.Text = $"ANALYZE · worst {design.Analysis.Worst} · mass {design.Analysis.TotalMassKg / 1000f:0.#} t";
        }));

        var exportScene = new Button { Content = "Export scene", Padding = new Thickness(10, 4) };
        exportScene.Click += (_, _) =>
        {
            var outDir = Path.Combine(design.DataRoot, "exports");
            Directory.CreateDirectory(outDir);
            var path = Path.Combine(outDir, "ship-analyze.nov3djson");
            var eval = ShipDesignEvaluator.Evaluate(design.Design, path);
            status.Text = $"Scene export · {eval.MeshNodeCount} meshes · {eval.CutoutCount} cutouts → {path}";
        };
        workspaceBar.Children.Add(exportScene);

        var create = ShipCreatePanel.Build(design, RefreshText);
        var decks = ShipDeckNavigator.Build(design);
        var objects = ShipPlanObjectList.Build(design);
        var props = ShipPropertyEditor.Build(design);

        var rightStack = new StackPanel { Spacing = 8 };
        rightStack.Children.Add(create);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(analysisPanel);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(decks);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(objects);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(props);
        rightStack.Children.Add(new Separator());
        rightStack.Children.Add(inspector);

        var right = new ScrollViewer
        {
            Width = 360,
            Content = rightStack,
            Background = new SolidColorBrush(Color.Parse("#1a1c20")),
        };

        var body = new DockPanel();
        DockPanel.SetDock(tools, Dock.Top);
        DockPanel.SetDock(workspaceBar, Dock.Top);
        DockPanel.SetDock(analysisStrip, Dock.Top);
        DockPanel.SetDock(snapRow, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        DockPanel.SetDock(right, Dock.Right);
        body.Children.Add(tools);
        body.Children.Add(workspaceBar);
        body.Children.Add(analysisStrip);
        body.Children.Add(snapRow);
        body.Children.Add(status);
        body.Children.Add(right);
        body.Children.Add(editorSurface);
        return body;
    }

    private static Button Ws(
        string label,
        ShipWorkspaceKind kind,
        ShipDesignSession design,
        TextBlock status,
        Action? onEnter = null)
    {
        var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
        btn.Click += (_, _) =>
        {
            design.SetWorkspace(kind);
            onEnter?.Invoke();
            if (onEnter is null)
            {
                status.Text = kind switch
                {
                    ShipWorkspaceKind.Plan => "PLAN — architecture & structure",
                    ShipWorkspaceKind.Model => "MODEL — CAD construction on selected object",
                    ShipWorkspaceKind.Analyze => "ANALYZE — engineering plausibility",
                    _ => label,
                };
            }
        };
        return btn;
    }
}
