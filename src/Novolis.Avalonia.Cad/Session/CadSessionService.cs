using Avalonia.Controls;
using Avalonia.Threading;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Ui;
using Novolis.Avalonia.Raylib;
using Novolis.Cad.Primitives;
using Novolis._3D;

namespace Novolis.Avalonia.Cad.Session;

/// <summary>
/// Single CAD executor shared by UI and HTTP/TCP. UI must call <see cref="Execute"/> for mutations.
/// </summary>
public sealed class CadSessionService : ICadSession
{
    private readonly CadDocumentSession _document;
    private readonly CadEditorSettings _settings;
    private readonly CadCommandBus _bus;
    private readonly CadCommandDispatcher _dispatcher;
    private readonly List<string> _recentExports = [];
    private readonly Dictionary<string, Func<CadCommandDto, CadCommandResultDto>> _extra = new(StringComparer.OrdinalIgnoreCase);
    private CadLastActionDto? _lastAction;
    private bool _subscribed;

    public CadSessionService(
        CadDocumentSession document,
        CadEditorSettings settings,
        CadCommandBus bus,
        CadCommandDispatcher dispatcher)
    {
        _document = document;
        _settings = settings;
        _bus = bus;
        _dispatcher = dispatcher;
        _document.Changed += () => RaiseChanged("document");
        _bus.Changed += () => RaiseChanged("commandBus");
        _dispatcher.ToolChanged += () => RaiseChanged("tool");
        _dispatcher.FitRequested += () => FitHandler?.Invoke();
        _dispatcher.SaveRequested += () => Execute(new CadCommandDto { ActionId = CadSessionActionIds.Save });
        _dispatcher.ElevationChanged += () => RaiseChanged("elevation");
        _dispatcher.SessionExecute = cmd =>
        {
            if (string.Equals(cmd.ActionId, CadSessionActionIds.RunCommand, StringComparison.OrdinalIgnoreCase))
                return Fail(CadSessionActionIds.RunCommand, "Nested runcommand is not allowed.", "nestedRun");
            return Execute(cmd);
        };
    }

    public CadDocumentSession Document => _document;

    public CadEditorSettings Settings => _settings;

    public CadCommandBus Bus => _bus;

    public CadCommandDispatcher Dispatcher => _dispatcher;

    /// <summary>Last in-memory scene from <c>bridgescene</c>.</summary>
    public SceneDocument? LastBridgedScene { get; private set; }

    /// <summary>Raised when <c>bridgescene</c> succeeds — App should load into Scene session.</summary>
    public event Action<SceneDocument>? SceneBridged;

    /// <summary>App-level Draft2D/Draft3D/Model/Stage switch (Cad Studio 3D host).</summary>
    public event Action<string>? StudioWorkspaceRequested;

    public CadEditorSurface? Editor
    {
        get => _editor;
        set
        {
            if (_editor is not null)
                _editor.ToolRequested -= OnEditorTool;
            _editor = value;
            if (_editor is not null)
            {
                _editor.ToolRequested += OnEditorTool;
                _editor.Tools.SessionExecute = Execute;
                _editor.PropertyPanel.SessionService = this;
            }
        }
    }

    private CadEditorSurface? _editor;

    private void OnEditorTool(string toolId) =>
        Execute(new CadCommandDto { ActionId = toolId });

    public CadPreviewControl? Preview { get; set; }

    /// <summary>When set, used instead of <see cref="Editor"/> draft viewport.</summary>
    public Control? PlanViewportControl { get; set; }

    /// <summary>When set, used instead of <see cref="Editor"/> / <see cref="Preview"/> host.</summary>
    public RaylibHostControl? ModelHostControl { get; set; }

    public RaylibHostControl? ModelHost => ModelHostControl ?? Editor?.ModelHost ?? Preview?.Host;

    public Control? PlanViewport => PlanViewportControl ?? Editor?.DraftViewport;

    public string? ExportRoot { get; set; }

    public string AppId { get; set; } = "novolis.cad";

    public string AppTitle { get; set; } = "Novolis CAD";

    public Action? FitHandler { get; set; }

    public Func<CadCommandDto, Task<CadCommandResultDto>>? AsyncExportHook { get; set; }

    public event Action<CadChangedEventDto>? Changed;

    public event Action<CadActionResultEventDto>? ActionResult;

    public void RegisterAction(string actionId, Func<CadCommandDto, CadCommandResultDto> handler) =>
        _extra[actionId] = handler;

    public CadHelloResponseDto Hello() => new()
    {
        AppId = AppId,
        AppTitle = AppTitle,
        ProcessId = Environment.ProcessId,
    };

    public CadActionsResponseDto Actions() => new() { Actions = BuildActions() };

    public CadSnapshotDto Snapshot() => BuildSnapshot();

    public void Subscribe() => _subscribed = true;

    public CadCommandResultDto Execute(CadCommandDto command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return InvokeOnUi(() => ExecuteCore(command));
    }

    private CadCommandResultDto ExecuteCore(CadCommandDto command)
    {
        var id = command.ActionId?.Trim() ?? "";
        CadCommandResultDto result;
        try
        {
            result = Dispatch(id, command);
        }
        catch (Exception ex)
        {
            result = Fail(id, ex.Message, "exception");
        }

        _lastAction = new CadLastActionDto
        {
            ActionId = result.ActionId,
            Ok = result.Ok,
            Message = result.Message,
            ErrorCode = result.ErrorCode,
        };
        result.Snapshot ??= BuildSnapshot();
        if (_subscribed)
        {
            ActionResult?.Invoke(new CadActionResultEventDto
            {
                ActionId = result.ActionId,
                Ok = result.Ok,
                Message = result.Message,
                ErrorCode = result.ErrorCode,
                Snapshot = result.Snapshot,
            });
        }

        RaiseChanged("command");
        return result;
    }

    private CadCommandResultDto Dispatch(string id, CadCommandDto command)
    {
        if (_extra.TryGetValue(id, out var extra))
            return extra(command);

        return id.ToLowerInvariant() switch
        {
            CadSessionActionIds.New => Do(id, () =>
            {
                _document.NewDocument();
                return "New document.";
            }),
            CadSessionActionIds.Open => Open(command),
            CadSessionActionIds.Save => Save(command),
            CadSessionActionIds.Undo => Do(id, () =>
            {
                _bus.Undo();
                return "Undo.";
            }),
            CadSessionActionIds.Redo => Do(id, () =>
            {
                _bus.Redo();
                return "Redo.";
            }),
            CadSessionActionIds.DeleteSelection => DeleteSelection(),
            CadSessionActionIds.Select => Select(command),
            CadSessionActionIds.Fit => Do(id, () =>
            {
                if (Editor is not null)
                    Editor.Fit();
                else
                    FitHandler?.Invoke();
                return "Fit.";
            }),
            CadSessionActionIds.SetTool => SetTool(command),
            CadSessionActionIds.SetViewMode => SetViewMode(command),
            CadSessionActionIds.SetWorkspace => SetWorkspace(command),
            CadSessionActionIds.SetSelectionMode => SetSelectionMode(command),
            CadSessionActionIds.SetElevation => SetElevation(command),
            CadSessionActionIds.SetSnap => SetSnap(command),
            CadSessionActionIds.SetGrid => SetGrid(command),
            CadSessionActionIds.SetAxisLock => SetAxisLock(command),
            CadSessionActionIds.RunCommand => RunCommand(command),
            CadSessionActionIds.ExportPlanPng => ExportPlan(command),
            CadSessionActionIds.ExportModelPng or CadSessionActionIds.ExportPreviewPng => ExportModel(command),
            CadSessionActionIds.ExportViewTour => ExportTour(command),
            CadSessionActionIds.ExportPhys => ExportPhys(command),
            CadSessionActionIds.Boolean => ActBoolean(command),
            CadSessionActionIds.Symmetry => ActSymmetry(command),
            CadSessionActionIds.Clone => ActClone(command),
            CadSessionActionIds.Instance => ActInstance(command),
            CadSessionActionIds.Connect => ActConnect(command),
            CadSessionActionIds.Split => ActSplit(command),
            CadSessionActionIds.Group => ActGroup(command),
            CadSessionActionIds.MeshFromSolid => ActMeshFromSolid(command),
            CadSessionActionIds.Weld => ActWeld(command),
            CadSessionActionIds.Optimize => ActOptimize(command),
            CadSessionActionIds.Bridge => ActBridge(command),
            CadSessionActionIds.AddMaterial => ActMaterial(command),
            CadSessionActionIds.AddLight => ActLight(command),
            CadSessionActionIds.AddCamera => ActCamera(command),
            CadSessionActionIds.SetStudioWorkspace => ActSetStudioWorkspace(command),
            CadSessionActionIds.ExportScene => CadDraftingActions.ExportScene(_document, command),
            CadSessionActionIds.BridgeScene => ActBridgeScene(command),
            CadSessionActionIds.SetMaterial => CadDraftingActions.SetMaterial(_bus, _document, command),
            CadSessionActionIds.SetWallSide => CadDraftingActions.SetWallSide(_bus, _document, command),
            CadSessionActionIds.AddWall => CadDraftingActions.AddWall(_bus, command),
            CadSessionActionIds.ExtrudeProfile => CadDraftingActions.ExtrudeProfile(_bus, command),
            CadSessionActionIds.AddDimension => CadDraftingActions.AddDimension(_bus, command),
            CadSessionActionIds.AddLine => CadDraftingActions.AddLine(_bus, command),
            CadSessionActionIds.AddCircle => CadDraftingActions.AddCircle(_bus, command),
            CadSessionActionIds.AddRect => CadDraftingActions.AddRect(_bus, command),
            CadSessionActionIds.AddSpline => CadDraftingActions.AddSpline(_bus, command),
            CadSessionActionIds.AddBox => CadDraftingActions.AddBox(_bus, command),
            _ => Fail(id, $"Unknown action '{id}'.", "unknownAction"),
        };
    }

    private CadCommandResultDto ActSetStudioWorkspace(CadCommandDto command)
    {
        var raw = command.Workspace
                  ?? command.Kind
                  ?? (command.Properties is not null
                      && command.Properties.TryGetValue("workspace", out var w) ? w : null);
        if (string.IsNullOrWhiteSpace(raw))
            return Fail(CadSessionActionIds.SetStudioWorkspace, "workspace required (draft2d|draft3d|model|stage).", "badWorkspace");

        var key = raw.Trim().ToLowerInvariant() switch
        {
            "draft2d" or "draft_2d" or "2d" or "cad" => "draft2d",
            "draft3d" or "draft_3d" or "3d" or "modeling" or "preview" => "draft3d",
            "model" or "mesh" => "model",
            "stage" or "render" or "stage/render" => "stage",
            _ => null,
        };
        if (key is null)
            return Fail(CadSessionActionIds.SetStudioWorkspace, $"Unknown workspace '{raw}'.", "badWorkspace");

        StudioWorkspaceRequested?.Invoke(key);
        return Ok(CadSessionActionIds.SetStudioWorkspace, $"Studio workspace → {key}.");
    }

    private CadCommandResultDto ActBridgeScene(CadCommandDto command)
    {
        var (result, scene) = CadDraftingActions.BridgeScene(_document, command);
        if (result.Ok)
        {
            LastBridgedScene = scene;
            SceneBridged?.Invoke(scene);
        }

        return result;
    }

    private CadCommandResultDto Open(CadCommandDto command)
    {
        if (string.IsNullOrWhiteSpace(command.Path) || !File.Exists(command.Path))
            return Fail(CadSessionActionIds.Open, "path required / missing.", "badPath");
        _document.OpenFromPath(command.Path);
        _settings.Save();
        return Ok(CadSessionActionIds.Open, $"Opened {Path.GetFileName(command.Path)}.");
    }

    private CadCommandResultDto Save(CadCommandDto command)
    {
        var path = string.IsNullOrWhiteSpace(command.Path) ? _document.DocumentPath : command.Path!;
        _document.SaveTo(path);
        _settings.Save();
        return Ok(CadSessionActionIds.Save, $"Saved {Path.GetFileName(path)}.");
    }

    private CadCommandResultDto DeleteSelection()
    {
        if (_document.SelectedId is not { } sid)
            return Fail(CadSessionActionIds.DeleteSelection, "Nothing selected.", "noSelection");
        _bus.Execute(new DeleteEntitiesCommand([sid]));
        return Ok(CadSessionActionIds.DeleteSelection, "Deleted.");
    }

    private CadCommandResultDto Select(CadCommandDto command)
    {
        var additive = command.Properties is not null
                       && command.Properties.TryGetValue("additive", out var a)
                       && string.Equals(a, "true", StringComparison.OrdinalIgnoreCase);
        _document.SetSelection(command.EntityId, additive);
        return Ok(CadSessionActionIds.Select, command.EntityId is null ? "Cleared selection." : $"Selected {command.EntityId}.");
    }

    private CadCommandResultDto SetTool(CadCommandDto command)
    {
        var tool = (command.Tool ?? "").Trim().ToLowerInvariant();
        var kind = tool switch
        {
            "line" => CadToolKind.Line,
            "circle" => CadToolKind.Circle,
            "rect" or "rectangle" => CadToolKind.Rect,
            "spline" => CadToolKind.Spline,
            "wall" => CadToolKind.Wall,
            "dimension" or "dim" => CadToolKind.Dimension,
            "select" or "" => CadToolKind.Select,
            _ => (CadToolKind?)null,
        };
        if (kind is null)
            return Fail(CadSessionActionIds.SetTool, $"Unknown tool '{command.Tool}'.", "badTool");
        _dispatcher.EnterTool(kind.Value);
        return Ok(CadSessionActionIds.SetTool, $"Tool {kind}.");
    }

    private CadCommandResultDto SetViewMode(CadCommandDto command)
    {
        // Legacy draft/model + new workspace names
        var workspace = CadWorkspaceMapping.Parse(command.Workspace ?? command.ViewMode);
        return ApplyWorkspace(CadSessionActionIds.SetViewMode, workspace);
    }

    private CadCommandResultDto SetWorkspace(CadCommandDto command)
    {
        var workspace = CadWorkspaceMapping.Parse(command.Workspace ?? command.ViewMode);
        return ApplyWorkspace(CadSessionActionIds.SetWorkspace, workspace);
    }

    private CadCommandResultDto ApplyWorkspace(string actionId, CadWorkspace workspace)
    {
        if (Editor is not null)
        {
            Editor.SetWorkspace(workspace);
            if (Editor.ModelRenderer is not null)
                Editor.ModelRenderer.Workspace = workspace;
        }
        else
            _settings.Settings.ViewMode = CadWorkspaceMapping.ToStorage(workspace);
        return Ok(actionId, $"Workspace {CadWorkspaceMapping.ToDisplay(workspace)}.");
    }

    private CadCommandResultDto SetSelectionMode(CadCommandDto command)
    {
        var mode = ParseSelectionMode(command.SelectionMode);
        if (Editor is not null)
            Editor.SetSelectionMode(mode);
        return Ok(CadSessionActionIds.SetSelectionMode, $"Selection {mode}.");
    }

    private static CadSelectionMode ParseSelectionMode(string? raw) =>
        (raw ?? "object").Trim().ToLowerInvariant() switch
        {
            "body" => CadSelectionMode.Body,
            "sketch" or "sketchelement" => CadSelectionMode.SketchElement,
            "island" or "meshisland" => CadSelectionMode.MeshIsland,
            "face" => CadSelectionMode.Face,
            "edge" => CadSelectionMode.Edge,
            "vertex" => CadSelectionMode.Vertex,
            "material" or "materialslot" => CadSelectionMode.MaterialSlot,
            "light" => CadSelectionMode.Light,
            "camera" => CadSelectionMode.Camera,
            _ => CadSelectionMode.Object,
        };

    private CadCommandResultDto ActBoolean(CadCommandDto command)
    {
        var target = command.TargetId ?? command.EntityId ?? _document.SelectedId;
        var cutter = command.CutterId
                     ?? (_document.SelectedIds.Count >= 2 ? _document.SelectedIds[1] : (Guid?)null);
        if (target is null || cutter is null)
            return Fail(CadSessionActionIds.Boolean, "Need targetId and cutterId (or two selected).", "badArgs");
        var id = CadModelingActions.AddBoolean(_bus, target.Value, cutter.Value, command.Operation ?? "subtract");
        return Ok(CadSessionActionIds.Boolean, $"Boolean {id}.");
    }

    private CadCommandResultDto ActSymmetry(CadCommandDto command)
    {
        var source = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (source is null)
            return Fail(CadSessionActionIds.Symmetry, "Need source selection.", "noSelection");
        var id = CadModelingActions.AddSymmetry(_bus, source.Value, merge: command.MergeAtPlane ?? true);
        return Ok(CadSessionActionIds.Symmetry, $"Symmetry {id}.");
    }

    private CadCommandResultDto ActClone(CadCommandDto command)
    {
        var source = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (source is null)
            return Fail(CadSessionActionIds.Clone, "Need source selection.", "noSelection");
        var realization = command.Realization ?? "instances";
        if (command.Axis is { Length: >= 3 } && command.StepRadians is { } step)
        {
            var count = command.Counts is { Length: >= 1 } ? command.Counts[0] : 6;
            var id = CadModelingActions.AddRadialClone(_bus, source.Value, count, command.Axis, step, realization);
            return Ok(CadSessionActionIds.Clone, $"Radial cloner {id}.");
        }

        var counts = command.Counts is { Length: >= 3 } ? command.Counts : [3, 1, 1];
        var spacing = command.Spacing is { Length: >= 3 } ? command.Spacing : [1f, 0f, 0f];
        var linearId = CadModelingActions.AddClone(_bus, source.Value, counts, spacing, realization);
        return Ok(CadSessionActionIds.Clone, $"Cloner {linearId}.");
    }

    private CadCommandResultDto ActInstance(CadCommandDto command)
    {
        var prototype = command.PrototypeId ?? command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (prototype is null)
            return Fail(CadSessionActionIds.Instance, "Need prototype selection.", "noSelection");
        CadTransform? xf = null;
        if (command.Center is { Length: >= 3 })
            xf = new CadTransform { Center = command.Center };
        var id = CadModelingActions.AddInstance(_bus, prototype.Value, xf);
        return Ok(CadSessionActionIds.Instance, $"Instance {id}.");
    }

    private CadCommandResultDto ActConnect(CadCommandDto command)
    {
        var members = command.MemberIds is { Length: > 0 }
            ? command.MemberIds
            : _document.SelectedIds.ToArray();
        if (members.Length < 1 && _document.SelectedId is { } one)
            members = [one];
        if (members.Length < 1)
            return Fail(CadSessionActionIds.Connect, "Need member ids.", "noSelection");
        var id = CadModelingActions.AddConnect(_bus, _document, members, command.Mode ?? "group");
        return Ok(CadSessionActionIds.Connect, $"Connect {id}.");
    }

    private CadCommandResultDto ActSplit(CadCommandDto command)
    {
        var source = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (source is null)
            return Fail(CadSessionActionIds.Split, "Need source selection.", "noSelection");
        var id = CadModelingActions.AddSplit(_bus, source.Value);
        return Ok(CadSessionActionIds.Split, $"Split {id}.");
    }

    private CadCommandResultDto ActGroup(CadCommandDto command)
    {
        var members = command.MemberIds is { Length: > 0 }
            ? command.MemberIds
            : _document.SelectedIds.ToArray();
        var id = CadModelingActions.AddGroup(_bus, _document, "Group", members.Length > 0 ? members : null);
        return Ok(CadSessionActionIds.Group, $"Group {id}.");
    }

    private CadCommandResultDto ActMeshFromSolid(CadCommandDto command)
    {
        var source = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (source is null)
            return Fail(CadSessionActionIds.MeshFromSolid, "Need CAD solid selection.", "noSelection");
        var id = CadModelingActions.AddMeshFromSolid(_bus, source.Value, command.LinkMode ?? "linked");
        return Ok(CadSessionActionIds.MeshFromSolid, $"MeshFromSolid {id}.");
    }

    private CadCommandResultDto ActWeld(CadCommandDto command)
    {
        var input = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (input is null)
            return Fail(CadSessionActionIds.Weld, "Need mesh input.", "noSelection");
        var id = CadModelingActions.AddWeld(_bus, input.Value, command.Tolerance ?? 1e-4f);
        return Ok(CadSessionActionIds.Weld, $"Weld {id}.");
    }

    private CadCommandResultDto ActOptimize(CadCommandDto command)
    {
        var input = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (input is null)
            return Fail(CadSessionActionIds.Optimize, "Need mesh input.", "noSelection");
        var id = CadModelingActions.AddOptimize(_bus, input.Value);
        return Ok(CadSessionActionIds.Optimize, $"Optimize {id}.");
    }

    private CadCommandResultDto ActBridge(CadCommandDto command)
    {
        var input = command.SourceId ?? command.EntityId ?? _document.SelectedId;
        if (input is null)
            return Fail(CadSessionActionIds.Bridge, "Need mesh input.", "noSelection");
        var id = CadModelingActions.AddBridge(_bus, input.Value);
        return Ok(CadSessionActionIds.Bridge, $"Bridge {id}.");
    }

    private CadCommandResultDto ActMaterial(CadCommandDto command)
    {
        var id = CadModelingActions.AddMaterial(_bus, command.EntityId ?? _document.SelectedId);
        return Ok(CadSessionActionIds.AddMaterial, $"Material {id}.");
    }

    private CadCommandResultDto ActLight(CadCommandDto command)
    {
        var id = CadModelingActions.AddLight(_bus, command.EntityId ?? _document.SelectedId);
        return Ok(CadSessionActionIds.AddLight, $"Light {id}.");
    }

    private CadCommandResultDto ActCamera(CadCommandDto command)
    {
        var id = CadModelingActions.AddCamera(_bus, command.EntityId ?? _document.SelectedId);
        return Ok(CadSessionActionIds.AddCamera, $"Camera {id}.");
    }

    private CadCommandResultDto SetElevation(CadCommandDto command)
    {
        if (command.Elevation is not { } y)
            return Fail(CadSessionActionIds.SetElevation, "elevation required.", "badElevation");
        _settings.Settings.DrawElevation = y;
        return Ok(CadSessionActionIds.SetElevation, $"Elevation {y}.");
    }

    private CadCommandResultDto SetSnap(CadCommandDto command)
    {
        if (command.Snap is not { } snap)
            return Fail(CadSessionActionIds.SetSnap, "snap required.", "badSnap");
        _settings.Settings.SnapToGrid = snap;
        return Ok(CadSessionActionIds.SetSnap, snap ? "Snap on." : "Snap off.");
    }

    private CadCommandResultDto SetGrid(CadCommandDto command)
    {
        if (command.GridStep is not { } step || step <= 0)
            return Fail(CadSessionActionIds.SetGrid, "gridStep required.", "badGrid");
        _settings.Settings.GridStep = step;
        return Ok(CadSessionActionIds.SetGrid, $"Grid {step}.");
    }

    private CadCommandResultDto SetAxisLock(CadCommandDto command)
    {
        var raw = command.Kind
                  ?? command.Tool
                  ?? (command.Properties is not null
                      && command.Properties.TryGetValue("axis", out var a) ? a : null)
                  ?? "none";
        var axis = raw.Trim().ToLowerInvariant() switch
        {
            "x" or "lockx" => "x",
            "y" or "locky" => "y",
            "z" or "lockz" => "z",
            "none" or "off" or "" => "none",
            _ => null,
        };
        if (axis is null)
            return Fail(CadSessionActionIds.SetAxisLock, "axis must be none|x|y|z.", "badAxis");
        _settings.Settings.AxisLock = axis;
        return Ok(CadSessionActionIds.SetAxisLock, $"Axis lock → {axis}.");
    }

    private CadCommandResultDto RunCommand(CadCommandDto command)
    {
        if (string.IsNullOrWhiteSpace(command.Prompt))
            return Fail(CadSessionActionIds.RunCommand, "prompt required.", "badPrompt");
        var msg = _dispatcher.TryDispatch(command.Prompt);
        // CadCommandDispatcher: null = success; non-null = error / usage message.
        if (msg is null)
            return Ok(CadSessionActionIds.RunCommand, "OK.");
        return Fail(CadSessionActionIds.RunCommand, msg, "dispatchFailed");
    }

    private CadCommandResultDto ExportPlan(CadCommandDto command)
    {
        var root = ResolveExportRoot(command);
        var path = string.IsNullOrWhiteSpace(command.Path)
            ? CadViewportExporter.AllocatePath(root, command.Kind ?? "plan")
            : command.Path!;
        if (PlanViewport is null)
            return Fail(CadSessionActionIds.ExportPlanPng, "No plan viewport.", "noPlan");
        var ok = InvokeOnUi(() => CadViewportExporter.TryExportPlanPng(PlanViewport, path));
        if (!ok)
            return Fail(CadSessionActionIds.ExportPlanPng, "Plan PNG failed.", "exportFailed");
        RememberExport(path);
        return Ok(CadSessionActionIds.ExportPlanPng, path, [path]);
    }

    private CadCommandResultDto ExportModel(CadCommandDto command)
    {
        var host = ModelHost;
        if (host is null)
            return Fail(command.ActionId, "No Raylib host.", "noHost");
        var root = ResolveExportRoot(command);
        var path = string.IsNullOrWhiteSpace(command.Path)
            ? CadViewportExporter.AllocatePath(root, command.Kind ?? "model")
            : command.Path!;
        var okPath = InvokeOnUiAsync(() => CadViewportExporter.ExportModelPngAsync(host, path)).GetAwaiter().GetResult();
        if (okPath is null)
            return Fail(command.ActionId, "Model PNG failed.", "exportFailed");
        RememberExport(okPath);
        return Ok(command.ActionId, okPath, [okPath]);
    }

    private CadCommandResultDto ExportTour(CadCommandDto command)
    {
        if (AsyncExportHook is not null)
            return AsyncExportHook(command).GetAwaiter().GetResult();
        return Fail(CadSessionActionIds.ExportViewTour, "No view tour hook registered.", "noTour");
    }

    private CadCommandResultDto ExportPhys(CadCommandDto command)
    {
        var path = string.IsNullOrWhiteSpace(command.Path)
            ? _settings.PhysDocumentPath
            : command.Path!;
        CadViewportExporter.ExportPhys(_document.Document, path, Path.GetFileName(_document.DocumentPath));
        RememberExport(path);
        return Ok(CadSessionActionIds.ExportPhys, path, [path]);
    }

    private string ResolveExportRoot(CadCommandDto command) =>
        !string.IsNullOrWhiteSpace(command.ExportRoot)
            ? command.ExportRoot!
            : ExportRoot ?? Path.Combine(_settings.DataRoot, "exports");

    private void RememberExport(string path)
    {
        _recentExports.Insert(0, path);
        while (_recentExports.Count > 12)
            _recentExports.RemoveAt(_recentExports.Count - 1);
    }

    private CadActionDto[] BuildActions() =>
    [
        A(CadSessionActionIds.New, "New", true),
        A(CadSessionActionIds.Open, "Open", true),
        A(CadSessionActionIds.Save, "Save", true),
        A(CadSessionActionIds.Undo, "Undo", _bus.CanUndo, _bus.CanUndo ? null : "Nothing to undo"),
        A(CadSessionActionIds.Redo, "Redo", _bus.CanRedo, _bus.CanRedo ? null : "Nothing to redo"),
        A(CadSessionActionIds.DeleteSelection, "Delete selection", _document.SelectedId is not null, "Nothing selected"),
        A(CadSessionActionIds.Select, "Select entity", true),
        A(CadSessionActionIds.Fit, "Fit", true),
        A(CadSessionActionIds.SetTool, "Set tool", true),
        A(CadSessionActionIds.SetViewMode, "Set view mode", true),
        A(CadSessionActionIds.SetWorkspace, "Set workspace", true),
        A(CadSessionActionIds.SetSelectionMode, "Set selection mode", true),
        A(CadSessionActionIds.SetElevation, "Set elevation", true),
        A(CadSessionActionIds.SetSnap, "Set snap", true),
        A(CadSessionActionIds.SetGrid, "Set grid step", true),
        A(CadSessionActionIds.SetAxisLock, "Lock move to axis (none|x|y|z)", true),
        A(CadSessionActionIds.RunCommand, "Run command DSL", true),
        A(CadSessionActionIds.ExportPlanPng, "Export plan PNG", PlanViewport is not null, "No plan viewport"),
        A(CadSessionActionIds.ExportModelPng, "Export model PNG", ModelHost is not null, "No model host"),
        A(CadSessionActionIds.ExportPreviewPng, "Export preview PNG", ModelHost is not null, "No preview host"),
        A(CadSessionActionIds.ExportViewTour, "Export view tour", AsyncExportHook is not null || ModelHost is not null, "No tour"),
        A(CadSessionActionIds.ExportPhys, "Export phys", true),
        A(CadSessionActionIds.Boolean, "Boolean", true),
        A(CadSessionActionIds.Symmetry, "Symmetry", true),
        A(CadSessionActionIds.Clone, "Cloner", true),
        A(CadSessionActionIds.Instance, "Instance", true),
        A(CadSessionActionIds.Connect, "Connect", true),
        A(CadSessionActionIds.Split, "Split", true),
        A(CadSessionActionIds.Group, "Group", true),
        A(CadSessionActionIds.MeshFromSolid, "Mesh From Solid", true),
        A(CadSessionActionIds.Weld, "Weld", true),
        A(CadSessionActionIds.Optimize, "Optimize", true),
        A(CadSessionActionIds.Bridge, "Bridge", true),
        A(CadSessionActionIds.AddMaterial, "Add material", true),
        A(CadSessionActionIds.AddLight, "Add light", true),
        A(CadSessionActionIds.AddCamera, "Add camera", true),
        A(CadSessionActionIds.SetStudioWorkspace, "Set studio workspace (draft2d|draft3d|model|stage)", true),
        A(CadSessionActionIds.ExportScene, "Export .nov3djson", true),
        A(CadSessionActionIds.BridgeScene, "Bridge to Scene (in-memory)", true),
        A(CadSessionActionIds.SetMaterial, "Set material on selection", _document.SelectedId is not null, "Nothing selected"),
        A(CadSessionActionIds.SetWallSide, "Set wall side shape", _document.SelectedId is not null, "Nothing selected"),
        A(CadSessionActionIds.AddWall, "Add wall", true),
        A(CadSessionActionIds.ExtrudeProfile, "Extrude profile", true),
        A(CadSessionActionIds.AddDimension, "Add dimension", true),
        A(CadSessionActionIds.AddLine, "Add line", true),
        A(CadSessionActionIds.AddCircle, "Add circle", true),
        A(CadSessionActionIds.AddRect, "Add rect", true),
        A(CadSessionActionIds.AddSpline, "Add spline", true),
        A(CadSessionActionIds.AddBox, "Add box", true),
        .._extra.Keys.Select(k => A(k, k, true)),
    ];

    private CadSnapshotDto BuildSnapshot()
    {
        var workspace = Editor?.Workspace
                        ?? CadWorkspaceMapping.Parse(_settings.Settings.ViewMode);
        return new()
        {
            DocumentName = _document.Document.Name,
            DocumentPath = _document.DocumentPath,
            Dirty = _document.IsDirty,
            EntityCount = _document.Document.Entities.Count,
            SelectedId = _document.SelectedId,
            SelectedIds = _document.SelectedIds.ToArray(),
            ActiveTool = _dispatcher.ActiveTool.ToString().ToLowerInvariant(),
            ViewMode = CadWorkspaceMapping.ToStorage(workspace),
            Workspace = CadWorkspaceMapping.ToStorage(workspace),
            SelectionMode = (Editor?.SelectionMode ?? CadSelectionMode.Object).ToString().ToLowerInvariant(),
            DrawElevation = _settings.Settings.DrawElevation,
            DisplayUnit = _settings.Settings.DisplayUnit,
            SnapToGrid = _settings.Settings.SnapToGrid,
            GridStep = _settings.Settings.GridStep,
            AxisLock = _settings.Settings.AxisLock,
            LastAction = _lastAction,
            RecentExportPaths = _recentExports.ToArray(),
            Actions = BuildActions(),
        };
    }

    private void RaiseChanged(string reason)
    {
        if (!_subscribed && Changed is null)
            return;
        Changed?.Invoke(new CadChangedEventDto { Reason = reason, Snapshot = BuildSnapshot() });
    }

    private static CadActionDto A(string id, string label, bool enabled, string? disabled = null) => new()
    {
        Id = id,
        Label = label,
        Enabled = enabled,
        DisabledReason = enabled ? null : disabled,
    };

    private static CadCommandResultDto Ok(string id, string message, string[]? paths = null) => new()
    {
        Ok = true,
        ActionId = id,
        Message = message,
        Paths = paths,
    };

    private static CadCommandResultDto Fail(string id, string message, string code) => new()
    {
        Ok = false,
        ActionId = id,
        Message = message,
        ErrorCode = code,
    };

    private static CadCommandResultDto Do(string id, Func<string> body) => Ok(id, body());

    private static T InvokeOnUi<T>(Func<T> body)
    {
        // Headless / unit tests have no Avalonia application — run inline.
        if (global::Avalonia.Application.Current is null)
            return body();
        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            return body();
        return global::Avalonia.Threading.Dispatcher.UIThread.Invoke(body);
    }

    private static Task<T> InvokeOnUiAsync<T>(Func<Task<T>> body)
    {
        if (global::Avalonia.Application.Current is null)
            return body();
        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            return body();
        var tcs = new TaskCompletionSource<T>();
        _ = global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try { tcs.SetResult(await body().ConfigureAwait(true)); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }
}
