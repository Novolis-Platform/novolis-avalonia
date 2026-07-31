using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Evaluation;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Raylib;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>
/// Viewport host: CAD plan (2D), Modeling draft 3D (Avalonia wireframe), Preview (Raylib shaded).
/// </summary>
public sealed class CadEditorSurface : Panel
{
    private readonly CadEditorSettings _settings;

    public CadEditorSurface(
        CadDocumentSession session,
        CadEditorSettings settings,
        CadCommandBus bus,
        CadCommandDispatcher dispatcher,
        CadToolController tools,
        CadModelRenderer modelRenderer)
    {
        Session = session;
        _settings = settings;
        CommandDispatcher = dispatcher;
        Tools = tools;
        ModelRenderer = modelRenderer;
        Bus = bus;
        Evaluator = new CadModelEvaluator();

        WorkspaceBar = new CadWorkspaceBar();
        SelectionModeBar = new CadSelectionModeBar();
        ToolStrip = new CadToolStrip();
        SceneTree = new CadSceneTree(session);
        PropertyPanel = new CadPropertyPanel(session);

        DraftViewport = new CadDraftViewport(session, settings, dispatcher, bus, tools);
        Draft3DViewport = new CadDraft3DViewport(session, settings, dispatcher, bus, tools);
        ModelHost = new RaylibHostControl();
        modelRenderer.Bind(ModelHost);
        modelRenderer.Evaluator = Evaluator;

        Children.Add(DraftViewport);
        Children.Add(Draft3DViewport);
        Children.Add(ModelHost);

        WorkspaceBar.WorkspaceChanged += SetWorkspace;
        SelectionModeBar.SelectionModeChanged += mode =>
        {
            SelectionMode = mode;
            SelectionModeChanged?.Invoke(mode);
        };
        ToolStrip.ToolRequested += id => ToolRequested?.Invoke(id);
        SceneTree.EntitySelected += _ => PropertyPanel.Refresh();

        ApplyWorkspace(CadWorkspaceMapping.Parse(settings.Settings.ViewMode));
    }

    public CadDocumentSession Session { get; }

    public CadCommandBus Bus { get; }

    public CadCommandDispatcher CommandDispatcher { get; }

    public CadToolController Tools { get; }

    public CadModelRenderer ModelRenderer { get; }

    public CadModelEvaluator Evaluator { get; }

    public CadDraftViewport DraftViewport { get; }

    /// <summary>Avalonia-native 3D drafting (box grid, snap, axis lock) — not Raylib.</summary>
    public CadDraft3DViewport Draft3DViewport { get; }

    public RaylibHostControl ModelHost { get; }

    public CadWorkspaceBar WorkspaceBar { get; }

    public CadSelectionModeBar SelectionModeBar { get; }

    public CadToolStrip ToolStrip { get; }

    public CadSceneTree SceneTree { get; }

    public CadPropertyPanel PropertyPanel { get; }

    public CadWorkspace Workspace { get; private set; } = CadWorkspace.Cad;

    public CadSelectionMode SelectionMode { get; private set; } = CadSelectionMode.Object;

    public CadViewMode ViewMode => CadWorkspaceMapping.ToViewMode(Workspace);

    public event Action? ViewModeChanged;

    public event Action<CadWorkspace>? WorkspaceChanged;

    public event Action<CadSelectionMode>? SelectionModeChanged;

    public event Action<string>? ToolRequested;

    public void SetWorkspace(CadWorkspace workspace)
    {
        if (Workspace == workspace)
            return;
        ApplyWorkspace(workspace);
        WorkspaceChanged?.Invoke(workspace);
        ViewModeChanged?.Invoke();
    }

    public void SetViewMode(CadViewMode mode) =>
        SetWorkspace(CadWorkspaceMapping.FromViewMode(mode));

    public void SetSelectionMode(CadSelectionMode mode)
    {
        SelectionModeBar.SelectionMode = mode;
        SelectionMode = mode;
    }

    public void Fit()
    {
        switch (Workspace)
        {
            case CadWorkspace.Cad:
                DraftViewport.Fit();
                break;
            case CadWorkspace.Modeling:
                Draft3DViewport.Fit();
                break;
            default:
                ModelRenderer.Fit();
                break;
        }
    }

    private void ApplyWorkspace(CadWorkspace workspace)
    {
        Workspace = workspace;
        _settings.Settings.ViewMode = CadWorkspaceMapping.ToStorage(workspace);
        WorkspaceBar.Workspace = workspace;
        SelectionModeBar.Workspace = workspace;
        ToolStrip.Workspace = workspace;
        SceneTree.Workspace = workspace;
        PropertyPanel.Workspace = workspace;
        SelectionMode = SelectionModeBar.SelectionMode;
        ModelRenderer.Workspace = workspace;

        DraftViewport.IsVisible = workspace == CadWorkspace.Cad;
        Draft3DViewport.IsVisible = workspace == CadWorkspace.Modeling;
        ModelHost.IsVisible = workspace == CadWorkspace.Preview;

        if (workspace == CadWorkspace.Preview)
        {
            ModelHost.SetHostActive(true);
            ModelHost.EnsureHostStarted();
            Evaluator.Invalidate();
            Evaluator.Evaluate(Session.Document);
        }
        else
        {
            ModelHost.SetHostActive(false);
            if (workspace == CadWorkspace.Modeling)
                Draft3DViewport.InvalidateVisual();
        }
    }
}
