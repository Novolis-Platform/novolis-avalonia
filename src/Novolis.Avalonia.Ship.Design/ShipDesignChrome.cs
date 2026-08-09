using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Session;
using Novolis.Avalonia.Cad.Ui;
using Novolis.Avalonia.Ship;
using Novolis.Avalonia.Ship.Design.Grips;
using Novolis.Avalonia.Ship.Design.Plan;
using Novolis.Avalonia.Ship.Design.Session;
using Novolis.Avalonia.Ship.Design.Services;
using Novolis.Avalonia.Ship.Design.Ui;
using Novolis.Cad.Primitives;
using Novolis.Ship.Analysis;
using Novolis.Ship.Design;

namespace Novolis.Avalonia.Ship.Design;

/// <summary>Object-first ship architect chrome: PLAN deck viewport + MODEL CAD + ANALYZE.</summary>
public static class ShipDesignChrome
{
    public static void Attach(CadSessionService cad, ShipDesignSession design)
    {
        ArgumentNullException.ThrowIfNull(cad);
        ArgumentNullException.ThrowIfNull(design);
        ShipChrome.Attach(cad);

        var syncDepth = 0;

        void SyncCadForModel()
        {
            if (design.Workspace != ShipWorkspaceKind.Model || !design.HasShip)
                return;
            syncDepth++;
            try
            {
                var source = design.SelectedObjectGeometry() ?? new CadDocument { Name = "empty", Entities = [] };
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

        design.Changed += SyncCadForModel;
        cad.Document.Changed += () =>
        {
            if (syncDepth > 0 || !design.HasShip)
                return;
            if (design.Workspace != ShipWorkspaceKind.Model)
                return;
            if (design.SelectedObjectId is not { } oid)
                return;

            var snap = new CadDocument
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

        var toolsController = new ShipArchitectToolController(design);
        var planViewport = new ShipDeckPlanViewport(design, toolsController);
        var analysisPanel = ShipAnalysisPanel.Build(design);
        analysisPanel.IsVisible = false;

        void SetStatus(string text)
        {
            design.SetStatusMessage(text);
            status.Text = text;
        }

        void RefreshText()
        {
            inspector.Text = design.HasShip
                ? ShipDesignInspector.Format(design)
                : "Clean slate — expand Create ship, then draw Wall / Room on the deck plan.";
            var val = design.Validation;
            var gripCount = design.HasShip
                ? ShipGripCatalog.ForSelection(design.Design, design.SelectedObjectId).Count
                : 0;
            var name = design.HasShip ? design.Design.Ship.Name : "(untitled)";
            var msg = design.StatusMessage;
            status.Text = string.IsNullOrWhiteSpace(msg)
                ? $"{design.Workspace} · {name} · deck {design.ActiveDeckIndex}"
                  + $" · {design.ActiveTool}"
                  + $" · val {(val.Ok ? "OK" : "FAIL")}({val.Issues.Count})"
                  + $" · {design.Analysis.Worst}"
                  + $" · grips {gripCount}"
                : msg;
            analysisPanel.IsVisible = design.Workspace == ShipWorkspaceKind.Analyze && design.HasShip;
        }

        design.Changed += RefreshText;
        RefreshText();

        CadEditorSurface? cadEditor = editorSurface as CadEditorSurface;
        if (cadEditor is not null)
        {
            cadEditor.IsVisible = false;
            cadEditor.ToolStrip.IsVisible = false;
        }

        void ApplyWorkspaceVisibility()
        {
            var model = design.Workspace == ShipWorkspaceKind.Model && design.HasShip;
            planViewport.IsVisible = !model;
            if (cadEditor is not null)
            {
                cadEditor.IsVisible = model;
                cadEditor.ToolStrip.IsVisible = model;
                if (model)
                {
                    cadEditor.SetWorkspace(CadWorkspace.Cad);
                    cadEditor.Fit();
                }
            }

            if (!model && design.HasShip)
                planViewport.Fit();
        }

        design.Changed += ApplyWorkspaceVisibility;

        var tools = ShipObjectToolStrip.Build(design, toolsController, SetStatus);
        var snapRow = ShipSnapSettings.Build(design);
        var analysisStrip = ShipAnalysisStatusStrip.Build(design, ids =>
        {
            design.SetHighlighted(ids);
            if (ids.Count > 0)
                design.Select(ids[0]);
            planViewport.InvalidateVisual();
        });

        var workspaceBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
        };
        workspaceBar.Children.Add(Ws("PLAN", ShipWorkspaceKind.Plan, design, () =>
        {
            SetStatus("PLAN — Wall / Room / Opening on the deck plan");
            planViewport.Fit();
        }));
        workspaceBar.Children.Add(Ws("MODEL", ShipWorkspaceKind.Model, design, () =>
        {
            SetStatus(design.SelectedObjectId is null
                ? "MODEL — select an object on PLAN first"
                : "MODEL — CAD construction on selection");
        }));
        workspaceBar.Children.Add(Ws("ANALYZE", ShipWorkspaceKind.Analyze, design, () =>
        {
            SetStatus(design.HasShip
                ? $"ANALYZE · worst {design.Analysis.Worst} · mass {design.Analysis.TotalMassKg / 1000f:0.#} t"
                : "ANALYZE — create a ship first");
        }));

        var exportScene = new Button { Content = "Export scene", Padding = new Thickness(10, 4) };
        exportScene.Click += (_, _) =>
        {
            if (!design.HasShip)
            {
                SetStatus("Create a ship before exporting.");
                return;
            }

            var outDir = Path.Combine(design.DataRoot, "exports");
            Directory.CreateDirectory(outDir);
            var path = Path.Combine(outDir, "ship-analyze.nov3djson");
            var eval = ShipDesignEvaluator.Evaluate(design.Design, path);
            SetStatus($"Scene export · {eval.MeshNodeCount} meshes · {eval.CutoutCount} cutouts → {path}");
        };
        workspaceBar.Children.Add(exportScene);

        var create = ShipCreatePanel.Build(design, () =>
        {
            RefreshText();
            SetStatus($"Created {design.Design.Ship.Name} — draw Wall / Room on the plan");
            design.SetActiveTool(ShipDesignTool.Bulkhead);
            toolsController.OnToolChanged();
            planViewport.Fit();
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

        var centerHost = new Panel();
        centerHost.Children.Add(planViewport);
        if (cadEditor is not null)
        {
            // Cad tool strip docked when MODEL; surface fills center.
            centerHost.Children.Add(cadEditor);
        }
        else
        {
            centerHost.Children.Add(editorSurface);
        }

        var body = new DockPanel();
        DockPanel.SetDock(tools, Dock.Top);
        if (cadEditor is not null)
            DockPanel.SetDock(cadEditor.ToolStrip, Dock.Top);
        DockPanel.SetDock(workspaceBar, Dock.Top);
        DockPanel.SetDock(analysisStrip, Dock.Top);
        DockPanel.SetDock(snapRow, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        DockPanel.SetDock(right, Dock.Right);
        body.Children.Add(tools);
        if (cadEditor is not null)
            body.Children.Add(cadEditor.ToolStrip);
        body.Children.Add(workspaceBar);
        body.Children.Add(analysisStrip);
        body.Children.Add(snapRow);
        body.Children.Add(status);
        body.Children.Add(right);
        body.Children.Add(centerHost);

        ApplyWorkspaceVisibility();
        return body;
    }

    private static Button Ws(string label, ShipWorkspaceKind kind, ShipDesignSession design, Action? onEnter)
    {
        var btn = new Button { Content = label, Padding = new Thickness(10, 4) };
        btn.Click += (_, _) =>
        {
            design.SetWorkspace(kind);
            onEnter?.Invoke();
        };
        return btn;
    }
}
