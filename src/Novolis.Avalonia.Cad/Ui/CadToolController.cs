using Avalonia;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;
using Novolis.Avalonia.Cad.Services;
using Novolis.Math.Geometry;
using System.Numerics;

namespace Novolis.Avalonia.Cad.Ui;

public sealed class CadToolController
{
    private readonly CadCommandDispatcher _dispatcher;
    private readonly CadEditorSettings _settings;
    private readonly List<Vector3> _points = [];
    private Vector3? _hover;
    private Vector3? _continuousAnchor;
    private bool _closeSplineHint;

    public CadToolController(CadCommandDispatcher dispatcher, CadEditorSettings settings)
    {
        _dispatcher = dispatcher;
        _settings = settings;
        _dispatcher.ToolChanged += () =>
        {
            if (_dispatcher.ActiveTool != CadToolKind.Line)
                _continuousAnchor = null;
            _points.Clear();
            _hover = null;
            _closeSplineHint = false;
            if (_dispatcher.ActiveTool == CadToolKind.Line && _settings.Settings.ContinuousLine && _continuousAnchor is { } anchor)
                _points.Add(anchor);
            Changed?.Invoke();
        };
    }

    /// <summary>When set, commits go through session Execute (agent parity); otherwise bus EmitAdd.</summary>
    public Func<CadCommandDto, CadCommandResultDto>? SessionExecute { get; set; }

    public event Action? Changed;

    public bool ContinuousLine
    {
        get => _settings.Settings.ContinuousLine;
        set
        {
            _settings.Settings.ContinuousLine = value;
            if (!value)
                _continuousAnchor = null;
            Changed?.Invoke();
        }
    }

    public string PromptHint => _dispatcher.ActiveTool switch
    {
        CadToolKind.Line when ContinuousLine && _points.Count > 0 =>
            "Line continuous: next point (Esc ends chain)",
        CadToolKind.Line => _points.Count == 0 ? "Line: first point" : "Line: second point",
        CadToolKind.Circle => _points.Count == 0 ? "Circle: center" : "Circle: radius point",
        CadToolKind.Rect => _points.Count == 0 ? "Rect: first corner" : "Rect: opposite corner",
        CadToolKind.Spline when _closeSplineHint =>
            "Spline: near start — click to close",
        CadToolKind.Spline => _points.Count == 0
            ? "Spline: points (near start closes · Enter finishes)"
            : $"Spline: {_points.Count} pts — click / near start closes / Enter",
        CadToolKind.Wall => _points.Count == 0 ? "Wall: first point" : "Wall: next point (Enter finishes)",
        CadToolKind.Dimension => _points.Count == 0 ? "Dimension: first point" : "Dimension: second point",
        _ => "Command:",
    };

    public bool IsCollectingSpline => _dispatcher.ActiveTool == CadToolKind.Spline && _points.Count > 0;

    public bool IsCollectingWall => _dispatcher.ActiveTool == CadToolKind.Wall && _points.Count > 0;

    public bool TryCommitWall()
    {
        if (_dispatcher.ActiveTool != CadToolKind.Wall || _points.Count < 2)
            return false;

        var pts = string.Join(';', _points.Select(VecProp));
        CommitViaSessionOrBus(
            CadSessionActionIds.AddWall,
            () => new CadEntity
            {
                Name = "Wall",
                Kind = "wall",
                Points = _points.Select(CadVec.From).ToList(),
                Height = 2.4f,
                Thickness = 0.15f,
            },
            new Dictionary<string, string>
            {
                ["points"] = pts,
                ["height"] = "2.4",
                ["thickness"] = "0.15",
            });
        _points.Clear();
        Changed?.Invoke();
        return true;
    }

    private void CommitViaSessionOrBus(
        string actionId,
        Func<CadEntity> buildEntity,
        Dictionary<string, string> properties)
    {
        if (SessionExecute is not null)
        {
            SessionExecute(new CadCommandDto
            {
                ActionId = actionId,
                Properties = properties,
            });
            return;
        }

        _dispatcher.EmitAdd(buildEntity());
    }

    private static string VecProp(Vector3 v) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{v.X},{v.Y},{v.Z}");

    public void Cancel()
    {
        _points.Clear();
        _hover = null;
        _continuousAnchor = null;
        _closeSplineHint = false;
        _dispatcher.EnterTool(CadToolKind.Select);
        Changed?.Invoke();
    }

    public bool TryCommitSpline(bool closed = false)
    {
        if (_dispatcher.ActiveTool != CadToolKind.Spline || _points.Count < 2)
            return false;

        var fit = _points.ToList();
        if (closed && fit.Count >= 3)
        {
            // Ensure last equals first for a closed loop.
            fit[^1] = fit[0];
        }

        var (degree, controls, knots, weights) = NurbsCurve.FromFitPoints(fit);
        _dispatcher.EmitAdd(new CadEntity
        {
            Name = "Spline",
            Kind = "spline",
            Degree = degree,
            ControlPoints = controls.Select(CadVec.From).ToList(),
            Knots = knots,
            Weights = weights,
            FitPoints = fit.Select(CadVec.From).ToList(),
            Closed = closed,
            Periodic = closed,
            Normal = [0f, 1f, 0f],
        }, keepTool: false);
        _points.Clear();
        _closeSplineHint = false;
        Changed?.Invoke();
        return true;
    }

    public void OnHover(Vector3 world)
    {
        _hover = world;
        _closeSplineHint = ShouldCloseSpline(world);
        Changed?.Invoke();
    }

    public void OnClick(Vector3 world, float pixelsPerMeter)
    {
        world = ApplyElevation(world);

        switch (_dispatcher.ActiveTool)
        {
            case CadToolKind.Line:
                HandleLineClick(world);
                break;

            case CadToolKind.Circle:
                _points.Add(world);
                if (_points.Count >= 2)
                {
                    var c = _points[0];
                    var r = Vector3.Distance(
                        new Vector3(c.X, 0, c.Z),
                        new Vector3(_points[1].X, 0, _points[1].Z));
                    _dispatcher.EmitAdd(new CadEntity
                    {
                        Name = "Circle",
                        Kind = "circle",
                        Center = CadVec.From(c),
                        Radius = r,
                        Normal = [0f, 1f, 0f],
                    });
                    _points.Clear();
                }

                break;

            case CadToolKind.Rect:
                _points.Add(world);
                if (_points.Count >= 2)
                {
                    var a = _points[0];
                    var b = _points[1];
                    _dispatcher.EmitAdd(new CadEntity
                    {
                        Name = "Rect",
                        Kind = "rect",
                        A = CadVec.From(a),
                        B = CadVec.Plan(b.X, b.Z, a.Y),
                        Normal = [0f, 1f, 0f],
                    });
                    _points.Clear();
                }

                break;

            case CadToolKind.Spline:
                if (ShouldCloseSpline(world) && _points.Count >= 2)
                {
                    TryCommitSpline(closed: true);
                    break;
                }

                _points.Add(world);
                break;

            case CadToolKind.Wall:
                _points.Add(world);
                break;

            case CadToolKind.Dimension:
                _points.Add(world);
                if (_points.Count >= 2)
                {
                    CommitViaSessionOrBus(
                        CadSessionActionIds.AddDimension,
                        () => new CadEntity
                        {
                            Name = "Dimension",
                            Kind = "dimension",
                            A = CadVec.From(_points[0]),
                            B = CadVec.From(_points[1]),
                            Height = 0.35f,
                        },
                        new Dictionary<string, string>
                        {
                            ["a"] = VecProp(_points[0]),
                            ["b"] = VecProp(_points[1]),
                            ["offset"] = "0.35",
                        });
                    _points.Clear();
                }

                break;
        }

        Changed?.Invoke();
        _ = pixelsPerMeter; // reserved for future pixel thresholds at call site
    }

    private void HandleLineClick(Vector3 world)
    {
        if (_points.Count == 0 && ContinuousLine && _continuousAnchor is { } anchor)
            _points.Add(ApplyElevation(anchor));

        _points.Add(world);
        if (_points.Count < 2)
            return;

        var a = _points[0];
        var b = _points[1];
        _dispatcher.EmitAdd(new CadEntity
        {
            Name = "Line",
            Kind = "line",
            A = CadVec.From(a),
            B = CadVec.From(b),
            Style = new CadStyle { Linetype = "Continuous" },
        }, keepTool: ContinuousLine);

        if (ContinuousLine)
        {
            _continuousAnchor = b;
            _points.Clear();
            _points.Add(b);
            _dispatcher.EnterTool(CadToolKind.Line);
        }
        else
        {
            _continuousAnchor = null;
            _points.Clear();
        }
    }

    private bool ShouldCloseSpline(Vector3 world)
    {
        if (_dispatcher.ActiveTool != CadToolKind.Spline || _points.Count < 2)
            return false;
        var first = _points[0];
        var dx = world.X - first.X;
        var dz = world.Z - first.Z;
        var dist = MathF.Sqrt(dx * dx + dz * dz);
        // ~10px at typical zoom ≈ handled by caller via world snap radius; use 0.15m default close radius
        // plus relative to span of points.
        var span = 0f;
        for (var i = 1; i < _points.Count; i++)
        {
            var d = Vector3.Distance(
                new Vector3(_points[0].X, 0, _points[0].Z),
                new Vector3(_points[i].X, 0, _points[i].Z));
            span = System.Math.Max(span, d);
        }

        var threshold = System.Math.Max(0.12f, span * 0.04f);
        return dist <= threshold;
    }

    private Vector3 ApplyElevation(Vector3 world) =>
        new(world.X, _settings.Settings.DrawElevation, world.Z);

    public void DrawPreview(DrawingContext context, Func<Vector3, Point> worldToScreen, double pixelsPerMeter)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 120, 200, 255)), 1.5, dashStyle: DashStyle.Dash);
        var closePen = new Pen(new SolidColorBrush(Color.FromArgb(220, 80, 220, 140)), 2);

        if (_dispatcher.ActiveTool == CadToolKind.Spline && _points.Count > 0)
        {
            var previewWorld = _hover is { } h
                ? (_closeSplineHint ? _points.Append(_points[0]).ToList() : _points.Append(ApplyElevation(h)).ToList())
                : _points;
            if (previewWorld.Count >= 2)
            {
                var (degree, controls, knots, weights) = NurbsCurve.FromFitPoints(previewWorld);
                var samples = NurbsCurve.Tessellate(degree, controls, knots, weights, 48);
                var usePen = _closeSplineHint ? closePen : pen;
                for (var i = 1; i < samples.Length; i++)
                    context.DrawLine(usePen, worldToScreen(samples[i - 1]), worldToScreen(samples[i]));
            }

            foreach (var p in _points)
            {
                var s = worldToScreen(p);
                context.DrawEllipse(Brushes.DeepSkyBlue, null, s, 3.5, 3.5);
            }

            if (_points.Count > 0)
            {
                var first = worldToScreen(_points[0]);
                var r = System.Math.Max(6, 10);
                context.DrawEllipse(null, _closeSplineHint ? closePen : pen, first, r, r);
            }

            return;
        }

        if (_points.Count == 0 || _hover is null)
            return;

        var a = _points[0];
        var b = ApplyElevation(_hover.Value);
        switch (_dispatcher.ActiveTool)
        {
            case CadToolKind.Line:
                context.DrawLine(pen, worldToScreen(a), worldToScreen(b));
                break;
            case CadToolKind.Circle:
            {
                var c = worldToScreen(a);
                var r = Vector3.Distance(
                    new Vector3(a.X, 0, a.Z),
                    new Vector3(b.X, 0, b.Z)) * pixelsPerMeter;
                context.DrawEllipse(null, pen, c, r, r);
                break;
            }
            case CadToolKind.Rect:
            {
                var p0 = worldToScreen(a);
                var p1 = worldToScreen(new Vector3(b.X, a.Y, a.Z));
                var p2 = worldToScreen(b);
                var p3 = worldToScreen(new Vector3(a.X, a.Y, b.Z));
                context.DrawLine(pen, p0, p1);
                context.DrawLine(pen, p1, p2);
                context.DrawLine(pen, p2, p3);
                context.DrawLine(pen, p3, p0);
                break;
            }
        }
    }
}