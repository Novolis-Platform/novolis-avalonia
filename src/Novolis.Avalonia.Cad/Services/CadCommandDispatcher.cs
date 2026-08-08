using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;
using Novolis.Commands.Expressions;
using Novolis.Math.Geometry;

namespace Novolis.Avalonia.Cad.Services;

public enum CadToolKind
{
    Select,
    Line,
    Circle,
    Rect,
    Spline,
    Wall,
    Dimension,
}

public sealed class CadCommandDispatcher
{
    private readonly CadDocumentSession _session;
    private readonly CadCommandBus _bus;
    private readonly CadEditorSettings _settings;

    public CadCommandDispatcher(CadDocumentSession session, CadCommandBus bus, CadEditorSettings settings)
    {
        _session = session;
        _bus = bus;
        _settings = settings;
    }

    /// <summary>
    /// Optional session Execute for verbs that map to catalogued actions
    /// (<c>exportscene</c>, <c>setmaterial</c>, <c>extrudeprofile</c>, …).
    /// </summary>
    public Func<CadCommandDto, CadCommandResultDto>? SessionExecute { get; set; }

    public CadToolKind ActiveTool { get; private set; } = CadToolKind.Select;

    public event Action? ToolChanged;

    public event Action? FitRequested;

    public event Action? SaveRequested;

    public event Action? DumpArtifactsRequested;

    public event Action? ElevationChanged;

    /// <summary>
    /// Dispatches a single call or a <c>;</c>-separated script
    /// (e.g. <c>Line(Point(0,1), Point(1,1)); Circle(Point(2,2), 0.5);</c>).
    /// Returns null on success, otherwise an error message.
    /// </summary>
    public string? TryDispatch(string prompt)
    {
        var script = FunctionCallParser.TryParseScript(prompt);
        if (!script.Success)
            return script.Message ?? "Could not parse command.";

        foreach (var call in script.Calls)
        {
            var err = DispatchOne(call);
            if (err is not null)
                return err;
        }

        return null;
    }

    private string? DispatchOne(FunctionCall call)
    {
        var name = call.Name.ToLowerInvariant();

        if (!call.HasParentheses || call.Arguments.Count == 0)
        {
            return name switch
            {
                "line" => EnterTool(CadToolKind.Line),
                "circle" => EnterTool(CadToolKind.Circle),
                "rect" or "rectangle" => EnterTool(CadToolKind.Rect),
                "spline" => EnterTool(CadToolKind.Spline),
                "wall" => EnterTool(CadToolKind.Wall),
                "dimension" or "dim" => EnterTool(CadToolKind.Dimension),
                "select" => EnterTool(CadToolKind.Select),
                "undo" => Do(() => _bus.Undo()),
                "redo" => Do(() => _bus.Redo()),
                "delete" => DeleteSelection(),
                "fit" => Do(() => FitRequested?.Invoke()),
                "save" => Do(() => SaveRequested?.Invoke()),
                "dump" or "dumpall" or "dumpmodel" or "dumpui" => Do(() => DumpArtifactsRequested?.Invoke()),
                "bridgescene" or "bridge" => ExecSession(CadSessionActionIds.BridgeScene),
                "snap" => "Snap(on|off) or Snap(true|false).",
                "axis" or "axislock" or "lock" => "AxisLock(none|x|y|z).",
                "exportscene" => "ExportScene(path) writes .nov3djson.",
                "level" or "elevation" or "zlevel" => "Level(y) sets drawing elevation (world Y).",
                "box" => "Box(w,h,d) or Box(x,y,z,w,h,d).",
                "extrude" or "extrudeprofile" => "Extrude(points…, height) or Extrude(height) on a rect selection.",
                "material" or "setmaterial" => "Material(name) or SetMaterial(name) on selection.",
                "workspace" or "setworkspace" => "Workspace(cad|modeling|preview).",
                "studio" or "setstudioworkspace" => "Studio(draft2d|draft3d|model|stage).",
                _ => ExecSessionOrUnknown(call.Name, name),
            };
        }

        return name switch
        {
            "line" => AddLine(call),
            "circle" => AddCircle(call),
            "rect" or "rectangle" => AddRect(call),
            "spline" => AddSpline(call),
            "wall" => AddWall(call),
            "dimension" or "dim" => AddDimension(call),
            "box" => AddBox(call),
            "cylinder" => AddCylinder(call),
            "sphere" => AddSphere(call),
            "move" => Move(call),
            "level" or "elevation" or "zlevel" => SetLevel(call),
            "extrude" or "extrudeprofile" => Extrude(call),
            "material" or "setmaterial" => SetMaterial(call),
            "exportscene" => ExportScene(call),
            "bridgescene" or "bridge" => ExecSession(CadSessionActionIds.BridgeScene),
            "snap" or "setsnap" => SetSnapCmd(call),
            "axis" or "axislock" or "lock" or "setaxislock" => SetAxisLockCmd(call),
            "workspace" or "setworkspace" => SessionWorkspace(CadSessionActionIds.SetWorkspace, call),
            "studio" or "setstudioworkspace" => SessionWorkspace(CadSessionActionIds.SetStudioWorkspace, call),
            "tool" or "settool" => SessionTool(call),
            "save" => Do(() => SaveRequested?.Invoke()),
            "dump" or "dumpall" or "dumpmodel" or "dumpui" => Do(() => DumpArtifactsRequested?.Invoke()),
            "delete" => DeleteSelection(),
            "undo" => Do(() => _bus.Undo()),
            "redo" => Do(() => _bus.Redo()),
            "fit" => Do(() => FitRequested?.Invoke()),
            "point" or "pt" or "vec" or "xyz" => "Point is a nested constructor, e.g. Line(Point(0,1), Point(1,1)).",
            _ => SessionForward(call) ?? $"Unknown command '{call.Name}'.",
        };
    }

    public void EmitAdd(CadEntity entity, bool keepTool = false)
    {
        _bus.Execute(new AddEntityCommand(entity));
        if (!keepTool)
            EnterTool(CadToolKind.Select);
    }

    public string? EnterTool(CadToolKind tool)
    {
        ActiveTool = tool;
        ToolChanged?.Invoke();
        return null;
    }

    private string? AddLine(FunctionCall call)
    {
        string? errA = null, errB = null;
        if (call.Arguments.Count == 2)
        {
            var okA = TryPoint(call.Arguments[0], out var a, out errA);
            var okB = TryPoint(call.Arguments[1], out var b, out errB);
            if (okA && okB)
            {
                _bus.Execute(new AddEntityCommand(new CadEntity
                {
                    Name = NextName("Line"),
                    Kind = "line",
                    A = a,
                    B = b,
                    Style = new CadStyle { Linetype = "Continuous" },
                }));
                return null;
            }

            if (errA is not null)
                return errA;
            if (errB is not null)
                return errB;
        }

        if (!RequireNumbers(call, 4, out var n, out var err))
            return err;
        var y = _settings.Settings.DrawElevation;
        _bus.Execute(new AddEntityCommand(new CadEntity
        {
            Name = NextName("Line"),
            Kind = "line",
            A = CadVec.Plan((float)n[0], (float)n[1], y),
            B = CadVec.Plan((float)n[2], (float)n[3], y),
            Style = new CadStyle { Linetype = "Continuous" },
        }));
        return null;
    }

    private string? AddCircle(FunctionCall call)
    {
        string? errPt = null;
        if (call.Arguments.Count == 2
            && TryPoint(call.Arguments[0], out var center, out errPt)
            && call.Arguments[1].Number is { } radius)
        {
            _bus.Execute(new AddEntityCommand(new CadEntity
            {
                Name = NextName("Circle"),
                Kind = "circle",
                Center = center,
                Radius = (float)radius,
                Normal = [0f, 1f, 0f],
            }));
            return null;
        }

        if (errPt is not null && call.Arguments.Count == 2)
            return errPt;

        if (!RequireNumbers(call, 3, out var n, out var err))
            return err;
        var y = _settings.Settings.DrawElevation;
        _bus.Execute(new AddEntityCommand(new CadEntity
        {
            Name = NextName("Circle"),
            Kind = "circle",
            Center = CadVec.Plan((float)n[0], (float)n[1], y),
            Radius = (float)n[2],
            Normal = [0f, 1f, 0f],
        }));
        return null;
    }

    private string? AddRect(FunctionCall call)
    {
        string? errA = null, errB = null;
        if (call.Arguments.Count == 2)
        {
            var okA = TryPoint(call.Arguments[0], out var a, out errA);
            var okB = TryPoint(call.Arguments[1], out var b, out errB);
            if (okA && okB)
            {
                _bus.Execute(new AddEntityCommand(new CadEntity
                {
                    Name = NextName("Rect"),
                    Kind = "rect",
                    A = a,
                    B = b,
                    Normal = [0f, 1f, 0f],
                }));
                return null;
            }

            if (errA is not null)
                return errA;
            if (errB is not null)
                return errB;
        }

        if (!RequireNumbers(call, 4, out var n, out var err))
            return err;
        var y = _settings.Settings.DrawElevation;
        _bus.Execute(new AddEntityCommand(new CadEntity
        {
            Name = NextName("Rect"),
            Kind = "rect",
            A = CadVec.Plan((float)n[0], (float)n[1], y),
            B = CadVec.Plan((float)n[2], (float)n[3], y),
            Normal = [0f, 1f, 0f],
        }));
        return null;
    }

    private string? AddWall(FunctionCall call)
    {
        // Wall(Point, Point) | Wall(Point, Point, thickness) | Wall(Point, Point, thickness, height)
        // Wall(x1,z1,x2,z2) | Wall(x1,z1,x2,z2,thickness,height)
        float? thickness = null;
        float? height = null;
        float[]? a;
        float[]? b;

        if (call.Arguments.Count is >= 2 and <= 4
            && TryPoint(call.Arguments[0], out a, out _)
            && TryPoint(call.Arguments[1], out b, out _))
        {
            if (call.Arguments.Count >= 3)
            {
                if (call.Arguments[2].Number is not { } t)
                    return "Wall thickness must be a number.";
                thickness = (float)t;
            }

            if (call.Arguments.Count >= 4)
            {
                if (call.Arguments[3].Number is not { } h)
                    return "Wall height must be a number.";
                height = (float)h;
            }
        }
        else if (call.Arguments.Count is 4 or 6)
        {
            if (!RequireNumbers(call, call.Arguments.Count, out var n, out var err))
                return err;
            var y = _settings.Settings.DrawElevation;
            a = CadVec.Plan((float)n[0], (float)n[1], y);
            b = CadVec.Plan((float)n[2], (float)n[3], y);
            if (n.Length >= 6)
            {
                thickness = (float)n[4];
                height = (float)n[5];
            }
        }
        else
        {
            return "Wall(Point(a), Point(b)[, thickness[, height]]) or Wall(x1,z1,x2,z2[, t, h]).";
        }

        _bus.Execute(new AddEntityCommand(new CadEntity
        {
            Name = NextName("Wall"),
            Kind = "wall",
            A = a,
            B = b,
            Thickness = thickness ?? 0.15f,
            Height = height ?? 2.4f,
            Deck = CadVec.DeckFromElevation(_settings.Settings.DrawElevation),
        }));
        return null;
    }

    private string? AddDimension(FunctionCall call)
    {
        string? errA = null, errB = null;
        if (call.Arguments.Count is 2 or 3)
        {
            var okA = TryPoint(call.Arguments[0], out var a, out errA);
            var okB = TryPoint(call.Arguments[1], out var b, out errB);
            if (okA && okB)
            {
                var offset = call.Arguments.Count == 3 && call.Arguments[2].Number is { } o
                    ? (float)o
                    : 0.35f;
                _bus.Execute(new AddEntityCommand(new CadEntity
                {
                    Name = NextName("Dimension"),
                    Kind = "dimension",
                    A = a,
                    B = b,
                    Height = offset,
                }));
                return null;
            }

            if (errA is not null)
                return errA;
            if (errB is not null)
                return errB;
        }

        if (call.Arguments.Count is not (4 or 5)
            || !RequireNumbers(call, call.Arguments.Count, out var n, out _))
            return "Dim(Point(a), Point(b)[, offset]) or Dim(x1,z1,x2,z2[, offset]).";

        var y = _settings.Settings.DrawElevation;
        _bus.Execute(new AddEntityCommand(new CadEntity
        {
            Name = NextName("Dimension"),
            Kind = "dimension",
            A = CadVec.Plan((float)n[0], (float)n[1], y),
            B = CadVec.Plan((float)n[2], (float)n[3], y),
            Height = n.Length >= 5 ? (float)n[4] : 0.35f,
        }));
        return null;
    }

    private string? AddSpline(FunctionCall call)
    {
        var y = _settings.Settings.DrawElevation;
        var fit = new List<Vector3>();

        if (call.Arguments.Count >= 2 && call.Arguments.All(a => a.IsCall))
        {
            foreach (var arg in call.Arguments)
            {
                if (!TryPoint(arg, out var p, out var err))
                    return err;
                fit.Add(CadVec.To(p));
            }
        }
        else
        {
            if (call.Arguments.Count < 4 || call.Arguments.Count % 2 != 0)
                return "Spline expects Point(...) args or an even number of XZ values.";

            for (var i = 0; i < call.Arguments.Count; i += 2)
            {
                if (call.Arguments[i].Number is not { } x || call.Arguments[i + 1].Number is not { } z)
                    return $"Spline arguments {i + 1}/{i + 2} must be numbers.";
                fit.Add(new Vector3((float)x, y, (float)z));
            }
        }

        var (degree, controls, knots, weights) = NurbsCurve.FromFitPoints(fit);
        _bus.Execute(new AddEntityCommand(new CadEntity
        {
            Name = NextName("Spline"),
            Kind = "spline",
            Degree = degree,
            ControlPoints = controls.Select(CadVec.From).ToList(),
            Knots = knots,
            Weights = weights,
            FitPoints = fit.Select(CadVec.From).ToList(),
            Closed = false,
            Normal = [0f, 1f, 0f],
        }));
        return null;
    }

    private string? Extrude(FunctionCall call)
    {
        if (SessionExecute is null)
            return "Extrude requires session Execute.";

        if (call.Arguments.Count == 1 && call.Arguments[0].Number is { } hOnly)
        {
            // Prefer selected rect footprint; fall back to last rect.
            var entity = _session.SelectedEntity
                         ?? _session.Document.Entities.LastOrDefault(e =>
                             e.Kind.Equals("rect", StringComparison.OrdinalIgnoreCase));
            if (entity?.A is null || entity.B is null)
                return "Extrude(height) needs a selected (or last) rect.";

            var a = CadVec.To(entity.A);
            var b = CadVec.To(entity.B);
            var points =
                $"{Fmt(a.X)},{Fmt(a.Y)},{Fmt(a.Z)};{Fmt(b.X)},{Fmt(a.Y)},{Fmt(a.Z)};{Fmt(b.X)},{Fmt(a.Y)},{Fmt(b.Z)};{Fmt(a.X)},{Fmt(a.Y)},{Fmt(b.Z)}";
            return SessionResult(SessionExecute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.ExtrudeProfile,
                Properties = new Dictionary<string, string>
                {
                    ["points"] = points,
                    ["height"] = Fmt(hOnly),
                },
            }));
        }

        // Extrude(Point, Point, …, height) — last arg height, rest footprint
        if (call.Arguments.Count >= 4 && call.Arguments[^1].Number is { } height)
        {
            var pts = new List<string>();
            for (var i = 0; i < call.Arguments.Count - 1; i++)
            {
                if (!TryPoint(call.Arguments[i], out var p, out var err))
                    return err;
                pts.Add($"{Fmt(p[0])},{Fmt(p[1])},{Fmt(p[2])}");
            }

            return SessionResult(SessionExecute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.ExtrudeProfile,
                Properties = new Dictionary<string, string>
                {
                    ["points"] = string.Join(';', pts),
                    ["height"] = Fmt(height),
                },
            }));
        }

        return "Extrude(height) or Extrude(Point…, height).";
    }

    private string? SetMaterial(FunctionCall call)
    {
        if (SessionExecute is null)
            return "Material requires session Execute.";
        if (call.Arguments.Count < 1)
            return "Material(name).";
        var name = call.Arguments[0].Text ?? call.Arguments[0].Raw;
        return SessionResult(SessionExecute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.SetMaterial,
            Kind = name,
        }));
    }

    private string? ExportScene(FunctionCall call)
    {
        if (SessionExecute is null)
            return "ExportScene requires session Execute.";
        if (call.Arguments.Count < 1)
            return "ExportScene(path).";
        var path = call.Arguments[0].Text ?? call.Arguments[0].Raw;
        return SessionResult(SessionExecute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.ExportScene,
            Path = path,
        }));
    }

    private string? SetSnapCmd(FunctionCall call)
    {
        if (call.Arguments.Count < 1)
            return "Snap(on|off).";
        var raw = (call.Arguments[0].Text ?? call.Arguments[0].Raw).Trim().ToLowerInvariant();
        var on = raw is "on" or "true" or "1" or "yes";
        var off = raw is "off" or "false" or "0" or "no";
        if (!on && !off)
            return "Snap(on|off).";
        if (SessionExecute is null)
        {
            _settings.Settings.SnapToGrid = on;
            return null;
        }

        return SessionResult(SessionExecute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.SetSnap,
            Snap = on,
        }));
    }

    private string? SetAxisLockCmd(FunctionCall call)
    {
        if (call.Arguments.Count < 1)
            return "AxisLock(none|x|y|z).";
        var axis = (call.Arguments[0].Text ?? call.Arguments[0].Raw).Trim().ToLowerInvariant();
        if (SessionExecute is null)
        {
            _settings.Settings.AxisLock = axis is "x" or "y" or "z" ? axis : "none";
            return null;
        }

        return SessionResult(SessionExecute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.SetAxisLock,
            Kind = axis,
        }));
    }

    private string? SessionWorkspace(string actionId, FunctionCall call)
    {
        if (SessionExecute is null)
            return "Workspace commands require session Execute.";
        if (call.Arguments.Count < 1)
            return $"{call.Name}(name).";
        var ws = call.Arguments[0].Text ?? call.Arguments[0].Raw;
        return SessionResult(SessionExecute(new CadCommandDto
        {
            ActionId = actionId,
            Workspace = ws,
            Kind = ws,
        }));
    }

    private string? SessionTool(FunctionCall call)
    {
        if (SessionExecute is null)
            return "Tool requires session Execute.";
        if (call.Arguments.Count < 1)
            return "Tool(name).";
        var tool = call.Arguments[0].Text ?? call.Arguments[0].Raw;
        return SessionResult(SessionExecute(new CadCommandDto
        {
            ActionId = CadSessionActionIds.SetTool,
            Tool = tool,
        }));
    }

    private string? ExecSession(string actionId)
    {
        if (SessionExecute is null)
            return $"'{actionId}' requires session Execute.";
        return SessionResult(SessionExecute(new CadCommandDto { ActionId = actionId }));
    }

    private string? ExecSessionOrUnknown(string displayName, string actionId)
    {
        if (SessionExecute is null)
            return $"Unknown command '{displayName}'.";
        var result = SessionExecute(new CadCommandDto { ActionId = actionId });
        if (!result.Ok
            && string.Equals(result.ErrorCode, "unknownAction", StringComparison.OrdinalIgnoreCase))
            return $"Unknown command '{displayName}'.";
        return SessionResult(result);
    }

    private string? SessionForward(FunctionCall call)
    {
        if (SessionExecute is null)
            return null;

        var dto = new CadCommandDto { ActionId = call.Name.ToLowerInvariant() };
        if (call.Arguments.Count > 0)
        {
            dto.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                var raw = call.Arguments[i].IsCall
                    ? call.Arguments[i].Call!.OriginalPrompt
                    : call.Arguments[i].Raw;
                dto.Properties[$"arg{i}"] = raw;
                if (i == 0)
                {
                    dto.Kind = call.Arguments[i].Text ?? raw;
                    dto.Path = call.Arguments[i].Text ?? raw;
                    dto.Workspace = call.Arguments[i].Text ?? raw;
                    dto.Tool = call.Arguments[i].Text ?? raw;
                    dto.Prompt = call.Arguments[i].Text ?? raw;
                }
            }
        }

        var result = SessionExecute(dto);
        if (!result.Ok && string.Equals(result.ErrorCode, "unknownAction", StringComparison.OrdinalIgnoreCase))
            return null;
        return SessionResult(result);
    }

    private static string? SessionResult(CadCommandResultDto result) =>
        result.Ok ? null : result.Message;

    private string? SetLevel(FunctionCall call)
    {
        if (!RequireNumbers(call, 1, out var n, out var err))
            return err;
        _settings.Settings.DrawElevation = (float)n[0];
        ElevationChanged?.Invoke();
        return null;
    }

    private string? AddBox(FunctionCall call)
    {
        if (call.Arguments.Count == 3)
        {
            if (!RequireNumbers(call, 3, out var n, out var err))
                return err;
            _bus.Execute(new AddEntityCommand(TagShipExterior(new CadEntity
            {
                Name = NextName("Box"),
                Kind = "box",
                Center = CadVec.Xyz(0, (float)n[1] * 0.5f, 0),
                HalfExtents = [(float)n[0] * 0.5f, (float)n[1] * 0.5f, (float)n[2] * 0.5f],
            })));
            return null;
        }

        if (!RequireNumbers(call, 6, out var m, out var err6))
            return err6;
        _bus.Execute(new AddEntityCommand(TagShipExterior(new CadEntity
        {
            Name = NextName("Box"),
            Kind = "box",
            Center = CadVec.Xyz((float)m[0], (float)m[1], (float)m[2]),
            HalfExtents = [(float)m[3] * 0.5f, (float)m[4] * 0.5f, (float)m[5] * 0.5f],
        })));
        return null;
    }

    private string? AddCylinder(FunctionCall call)
    {
        if (call.Arguments.Count == 2)
        {
            if (!RequireNumbers(call, 2, out var n, out var err))
                return err;
            _bus.Execute(new AddEntityCommand(TagShipExterior(new CadEntity
            {
                Name = NextName("Cylinder"),
                Kind = "cylinder",
                Center = CadVec.Xyz(0, (float)n[1] * 0.5f, 0),
                Radius = (float)n[0],
                Height = (float)n[1],
            })));
            return null;
        }

        if (call.Arguments.Count == 5)
        {
            if (!RequireNumbers(call, 5, out var n, out var err))
                return err;
            _bus.Execute(new AddEntityCommand(TagShipExterior(new CadEntity
            {
                Name = NextName("Cylinder"),
                Kind = "cylinder",
                Center = CadVec.Xyz((float)n[0], (float)n[1], (float)n[2]),
                Radius = (float)n[3],
                Height = (float)n[4],
            })));
            return null;
        }

        return "Cylinder(r,h) or Cylinder(x,y,z,r,h).";
    }

    private string? AddSphere(FunctionCall call)
    {
        if (call.Arguments.Count == 1)
        {
            if (!RequireNumbers(call, 1, out var n, out var err))
                return err;
            _bus.Execute(new AddEntityCommand(TagShipExterior(new CadEntity
            {
                Name = NextName("Sphere"),
                Kind = "sphere",
                Center = CadVec.Xyz(0, (float)n[0], 0),
                Radius = (float)n[0],
            })));
            return null;
        }

        if (!RequireNumbers(call, 4, out var m, out var err4))
            return err4;
        _bus.Execute(new AddEntityCommand(TagShipExterior(new CadEntity
        {
            Name = NextName("Sphere"),
            Kind = "sphere",
            Center = CadVec.Xyz((float)m[0], (float)m[1], (float)m[2]),
            Radius = (float)m[3],
        })));
        return null;
    }

    private string? Move(FunctionCall call)
    {
        if (!RequireNumbers(call, 3, out var n, out var err))
            return err;
        if (_session.SelectedId is null)
            return "Nothing selected.";
        _bus.Execute(new MoveEntitiesCommand([_session.SelectedId.Value], (float)n[0], (float)n[1], (float)n[2]));
        return null;
    }

    private string? DeleteSelection()
    {
        if (_session.SelectedId is null)
            return "Nothing selected.";
        _bus.Execute(new DeleteEntitiesCommand([_session.SelectedId.Value]));
        return null;
    }

    private string? Do(Action action)
    {
        action();
        return null;
    }

    private string NextName(string prefix)
    {
        var n = _session.Document.Entities.Count(e =>
            (e.Name ?? string.Empty).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) + 1;
        return $"{prefix} {n}";
    }

    private bool TryPoint(ExpressionArg arg, out float[] xyz, out string? error)
    {
        xyz = [];
        error = null;
        var y = _settings.Settings.DrawElevation;

        if (arg.Call is { } call)
        {
            var n = call.Name.ToLowerInvariant();
            if (n is not ("point" or "pt" or "vec" or "xyz" or "pos"))
            {
                error = $"Expected Point(...), got {call.Name}(...).";
                return false;
            }

            if (call.Arguments.Count == 2
                && call.Arguments[0].Number is { } x2
                && call.Arguments[1].Number is { } z2)
            {
                xyz = CadVec.Plan((float)x2, (float)z2, y);
                return true;
            }

            if (call.Arguments.Count == 3
                && call.Arguments[0].Number is { } x3
                && call.Arguments[1].Number is { } y3
                && call.Arguments[2].Number is { } z3)
            {
                xyz = CadVec.Xyz((float)x3, (float)y3, (float)z3);
                return true;
            }

            error = "Point(x,z) or Point(x,y,z).";
            return false;
        }

        error = "Expected Point(...).";
        return false;
    }

    private static bool RequireNumbers(FunctionCall call, int count, out double[] numbers, out string? error)
    {
        numbers = new double[count];
        if (call.Arguments.Count != count)
        {
            error = $"'{call.Name}' expects {count} number argument(s), got {call.Arguments.Count}.";
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (call.Arguments[i].Number is not { } value)
            {
                error = $"Argument {i + 1} must be a number (got '{call.Arguments[i].Raw}').";
                numbers = [];
                return false;
            }

            numbers[i] = value;
        }

        error = null;
        return true;
    }

    private CadEntity TagShipExterior(CadEntity entity)
    {
        if (!CadVec.LooksLikeShipDocument(_session.Document))
            return entity;
        entity.Properties ??= new Dictionary<string, JsonElement>();
        entity.Properties["exterior"] = JsonSerializer.SerializeToElement(true);
        return entity;
    }

    private static string Fmt(double value) =>
        value.ToString("0.####", CultureInfo.InvariantCulture);

    public static string FormatInvariant(double value) => Fmt(value);
}
