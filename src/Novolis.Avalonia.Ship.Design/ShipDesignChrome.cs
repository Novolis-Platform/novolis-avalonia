using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Cad.Ui;
using Novolis.Avalonia.Ship;
using Novolis.Avalonia.Ship.Design.Grips;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Avalonia.Ship.Design.Services;
using Novolis.Avalonia.Ship.Design.Ui;
using Novolis.Cad.Primitives;
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

        var syncDepth = 0;

        void SyncCad()
        {
            syncDepth++;
            try
            {
                if (!design.HasShip)
                {
                    cad.Document.Document.Entities.Clear();
                    cad.Document.Document.Name = "(untitled)";
                    cad.Document.Document.Properties = new Dictionary<string, System.Text.Json.JsonElement>();
                    cad.Document.Notify();
                    return;
                }

                var source = design.Workspace == ShipWorkspaceKind.Model
                    ? design.SelectedObjectGeometry() ?? ShipCadProjector.ToCadDocument(design.Design)
                    : ShipCadProjector.ToCadDocument(design.Design);

                cad.Document.Document.Entities.Clear();
                cad.Document.Document.Entities.AddRange(source.Entities);
                cad.Document.Document.Name = source.Name;
                cad.Document.Document.Properties = source.Properties;
                cad.Document.Notify();
            }
            finally
            {
                syncDepth--;
            }
        }

        void SyncDrawElevation()
        {
            if (!design.HasShip || design.Design.Decks.Count == 0)
                return;
            var deck = design.Design.Decks[
                System.Math.Clamp(design.ActiveDeckIndex, 0, design.Design.Decks.Count - 1)];
            // CadVec bands decks at 3.6 m steps; stamp elevation so IsolateLevel matches Deck index.
            cad.Settings.Settings.DrawElevation = deck.Index * Novolis.Cad.Primitives.CadVec.DeckHeightMeters;
            cad.Settings.Settings.IsolateLevel = true;
        }

        design.Changed += () =>
        {
            SyncDrawElevation();
            SyncCad();
        };
        SyncDrawElevation();
        cad.Document.Changed += () =>
        {
            if (syncDepth > 0 || !design.HasShip)
                return;
            if (design.Workspace != ShipWorkspaceKind.Model)
                return;
            if (design.SelectedObjectId is not { } oid)
                return;

            var snap = new Novolis.Cad.Primitives.CadDocument
            {
                Name = cad.Document.Document.Name,
                Entities = cad.Document.Document.Entities.ToList(),
                Properties = cad.Document.Document.Properties,
            };
            syncDepth++;
            try
            {
                design.Mutate(d => ShipDesignMutations.ReplaceObjectGeometry(d, oid, snap));
            }
            finally
            {
                syncDepth--;
            }
        };

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

        void SetStatus(string text) => status.Text = text;

        void RefreshText()
        {
            inspector.Text = design.HasShip
                ? ShipDesignInspector.Format(design)
                : "Clean slate — use Create ship, then PLAN tools (click to place) or Place default.";
            var val = design.Validation;
            var gripCount = design.HasShip
                ? ShipGripCatalog.ForSelection(design.Design, design.SelectedObjectId).Count
                : 0;
            var name = design.HasShip ? design.Design.Ship.Name : "(untitled)";
            status.Text =
                $"{design.Workspace} · {name} · deck {design.ActiveDeckIndex}"
                + $" · tool {design.ActiveTool}"
                + $" · val {(val.Ok ? "OK" : "FAIL")}({val.Issues.Count})"
                + $" · analysis {design.Analysis.Worst}"
                + $" · grips {gripCount}";
            analysisPanel.IsVisible = design.Workspace == ShipWorkspaceKind.Analyze && design.HasShip;
        }

        design.Changed += RefreshText;
        RefreshText();

        var tools = ShipObjectToolStrip.Build(design, SetStatus);
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
            status.Text = design.HasShip
                ? $"ANALYZE · worst {design.Analysis.Worst} · mass {design.Analysis.TotalMassKg / 1000f:0.#} t"
                : "ANALYZE — create a ship first";
        }));

        var exportScene = new Button { Content = "Export scene", Padding = new Thickness(10, 4) };
        exportScene.Click += (_, _) =>
        {
            if (!design.HasShip)
            {
                status.Text = "Create a ship before exporting.";
                return;
            }

            var outDir = Path.Combine(design.DataRoot, "exports");
            Directory.CreateDirectory(outDir);
            var path = Path.Combine(outDir, "ship-analyze.nov3djson");
            var eval = ShipDesignEvaluator.Evaluate(design.Design, path);
            status.Text = $"Scene export · {eval.MeshNodeCount} meshes · {eval.CutoutCount} cutouts → {path}";
        };
        workspaceBar.Children.Add(exportScene);

        Control? cadToolStrip = null;
        if (editorSurface is CadEditorSurface editor)
        {
            cadToolStrip = editor.ToolStrip;
            editor.ToolStrip.IsVisible = false;
            editor.DraftViewport.WorldClickFilter = world =>
            {
                if (!design.HasShip || design.Workspace != ShipWorkspaceKind.Plan)
                    return false;
                if (design.ActiveTool is ShipDesignTool.Select or ShipDesignTool.Hull or ShipDesignTool.Structure)
                    return false;
                var msg = ShipPlanAuthoring.TryHandleWorldClick(design, world);
                if (msg is null)
                    return false;
                SetStatus(msg);
                return true;
            };
            void SyncCadWorkspace()
            {
                var planOrModel = design.Workspace is ShipWorkspaceKind.Plan or ShipWorkspaceKind.Model;
                editor.ToolStrip.IsVisible = design.Workspace == ShipWorkspaceKind.Model && design.HasShip;
                if (planOrModel)
                    editor.SetWorkspace(CadWorkspace.Cad);
            }

            design.Changed += SyncCadWorkspace;
            SyncCadWorkspace();
        }

        var create = ShipCreatePanel.Build(design, () =>
        {
            RefreshText();
            SetStatus($"Created {design.Design.Ship.Name} — click Passage/Bulkhead/… to place (shows on deck plan).");
            if (editorSurface is CadEditorSurface ed)
            {
                ed.SetWorkspace(CadWorkspace.Cad);
                ed.Fit();
            }
        });
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
        if (cadToolStrip is not null)
            DockPanel.SetDock(cadToolStrip, Dock.Top);
        DockPanel.SetDock(workspaceBar, Dock.Top);
        DockPanel.SetDock(analysisStrip, Dock.Top);
        DockPanel.SetDock(snapRow, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        DockPanel.SetDock(right, Dock.Right);
        body.Children.Add(tools);
        if (cadToolStrip is not null)
            body.Children.Add(cadToolStrip);
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
                    ShipWorkspaceKind.Plan => design.HasShip
                        ? "PLAN — pick a tool, click the deck plan to place"
                        : "PLAN — create a ship first",
                    ShipWorkspaceKind.Model => design.HasShip
                        ? "MODEL — select an object, then use CAD Line/Wall/Rect tools"
                        : "MODEL — create a ship first",
                    ShipWorkspaceKind.Analyze => "ANALYZE — engineering plausibility",
                    _ => label,
                };
            }
        };
        return btn;
    }
}
