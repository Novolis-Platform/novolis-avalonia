using Avalonia.Controls;
using Avalonia.Threading;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Cad.Ui;
using Novolis.Avalonia.Raylib;
using Novolis.Cad.Primitives;

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
    }

    public CadDocumentSession Document => _document;

    public CadEditorSettings Settings => _settings;

    public CadCommandBus Bus => _bus;

    public CadCommandDispatcher Dispatcher => _dispatcher;

    public CadEditorSurface? Editor { get; set; }

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
            CadSessionActionIds.SetElevation => SetElevation(command),
            CadSessionActionIds.SetSnap => SetSnap(command),
            CadSessionActionIds.SetGrid => SetGrid(command),
            CadSessionActionIds.RunCommand => RunCommand(command),
            CadSessionActionIds.ExportPlanPng => ExportPlan(command),
            CadSessionActionIds.ExportModelPng or CadSessionActionIds.ExportPreviewPng => ExportModel(command),
            CadSessionActionIds.ExportViewTour => ExportTour(command),
            CadSessionActionIds.ExportPhys => ExportPhys(command),
            _ => Fail(id, $"Unknown action '{id}'.", "unknownAction"),
        };
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
        _document.SelectedId = command.EntityId;
        _document.Notify();
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
        var mode = string.Equals(command.ViewMode, "model", StringComparison.OrdinalIgnoreCase)
            ? CadViewMode.Model
            : CadViewMode.Draft;
        if (Editor is not null)
            Editor.SetViewMode(mode);
        else
            _settings.Settings.ViewMode = mode == CadViewMode.Model ? "model" : "draft";
        return Ok(CadSessionActionIds.SetViewMode, $"View {mode}.");
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

    private CadCommandResultDto RunCommand(CadCommandDto command)
    {
        if (string.IsNullOrWhiteSpace(command.Prompt))
            return Fail(CadSessionActionIds.RunCommand, "prompt required.", "badPrompt");
        var msg = _dispatcher.TryDispatch(command.Prompt);
        return msg is null || msg.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase)
            ? Fail(CadSessionActionIds.RunCommand, msg ?? "Failed.", "dispatchFailed")
            : Ok(CadSessionActionIds.RunCommand, msg);
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
        A(CadSessionActionIds.SetElevation, "Set elevation", true),
        A(CadSessionActionIds.SetSnap, "Set snap", true),
        A(CadSessionActionIds.SetGrid, "Set grid", true),
        A(CadSessionActionIds.RunCommand, "Run command DSL", true),
        A(CadSessionActionIds.ExportPlanPng, "Export plan PNG", PlanViewport is not null, "No plan viewport"),
        A(CadSessionActionIds.ExportModelPng, "Export model PNG", ModelHost is not null, "No model host"),
        A(CadSessionActionIds.ExportPreviewPng, "Export preview PNG", ModelHost is not null, "No preview host"),
        A(CadSessionActionIds.ExportViewTour, "Export view tour", AsyncExportHook is not null || ModelHost is not null, "No tour"),
        A(CadSessionActionIds.ExportPhys, "Export phys", true),
        .._extra.Keys.Select(k => A(k, k, true)),
    ];

    private CadSnapshotDto BuildSnapshot() => new()
    {
        DocumentName = _document.Document.Name,
        DocumentPath = _document.DocumentPath,
        Dirty = _document.IsDirty,
        EntityCount = _document.Document.Entities.Count,
        SelectedId = _document.SelectedId,
        ActiveTool = _dispatcher.ActiveTool.ToString().ToLowerInvariant(),
        ViewMode = Editor?.ViewMode == CadViewMode.Model
            ? "model"
            : (_settings.Settings.ViewMode ?? "draft"),
        DrawElevation = _settings.Settings.DrawElevation,
        DisplayUnit = _settings.Settings.DisplayUnit,
        SnapToGrid = _settings.Settings.SnapToGrid,
        GridStep = _settings.Settings.GridStep,
        LastAction = _lastAction,
        RecentExportPaths = _recentExports.ToArray(),
        Actions = BuildActions(),
    };

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
        if (global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            return body();
        return global::Avalonia.Threading.Dispatcher.UIThread.Invoke(body);
    }

        private static Task<T> InvokeOnUiAsync<T>(Func<Task<T>> body)
    {
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
