using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis._3D;

namespace Novolis.Avalonia._3D.Ui;

public sealed class PropertyInspectorControl : UserControl
{
    private readonly SceneSessionService _session;
    private readonly StackPanel _body = new() { Margin = new Thickness(8), Spacing = 6 };

    public PropertyInspectorControl(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Content = new DockPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Properties",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 8, 8, 4),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                new ScrollViewer { Content = _body },
            },
        };
        _session.DocumentChanged += Refresh;
        Refresh();
    }

    /// <summary>Main viewport camera — used by Match Viewport.</summary>
    public SceneViewportCamera? ViewportCamera { get; set; }

    private void MatchViewport(CameraNode cam)
    {
        if (ViewportCamera is null)
        {
            _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.MatchViewport,
                NodeId = cam.Id.ToString(),
            });
            return;
        }

        var eye = ViewportCamera.Orbit.BuildEyePosition();
        var target = ViewportCamera.Orbit.Target;
        _session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.MatchViewport,
            NodeId = cam.Id.ToString(),
            X = eye.X,
            Y = eye.Y,
            Z = eye.Z,
            Rx = target.X,
            Ry = target.Y,
            Rz = target.Z,
            Distance = ViewportCamera.Orbit.FieldOfViewDegrees,
        });

        // Keep shaded preview in sync with the CAD view we just captured.
        SceneRenderActions.SyncOpenPreviewFromMain();
    }

    public void Refresh()
    {
        _body.Children.Clear();
        if (_session.Document.SelectionId is not { } id || _session.Document.Find(id) is not { } node)
        {
            _body.Children.Add(new TextBlock { Text = "Nothing selected.", Opacity = 0.7 });
            return;
        }

        _body.Children.Add(Label($"Name: {node.Name}"));
        _body.Children.Add(Label($"Kind: {node.GetType().Name}"));
        _body.Children.Add(Label(
            $"Pos: {node.Transform.Position[0]:0.###}, {node.Transform.Position[1]:0.###}, {node.Transform.Position[2]:0.###}"));

        if (node is LightNode light)
        {
            _body.Children.Add(Label($"Light: {light.LightKind}"));
            _body.Children.Add(Label($"Intensity: {light.Intensity:0.##}"));
            _body.Children.Add(Label($"Enabled: {light.Enabled}"));
            if (light.LightKind == LightKind.Spot)
                _body.Children.Add(Label($"Cone: {light.ConeAngleDeg:0.#}° / penumbra {light.PenumbraDeg:0.#}°"));
            if (light.LightKind == LightKind.Area)
                _body.Children.Add(Label($"Area: {light.AreaSize[0]:0.##} × {light.AreaSize[1]:0.##}"));

            _body.Children.Add(IntensitySlider(light));
        }

        if (node is CameraNode cam)
        {
            _body.Children.Add(Label($"FOV: {cam.FovDeg:0.#}°"));
            _body.Children.Add(Label(
                $"Target: {cam.Target[0]:0.###}, {cam.Target[1]:0.###}, {cam.Target[2]:0.###}"));

            var lookThrough = new Button
            {
                Content = "Look Through",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            ToolTip.SetTip(lookThrough, "Snap the main viewport (and open Render preview) to this camera.");
            lookThrough.Click += (_, _) =>
            {
                _session.Execute(new AgentCommand
                {
                    ActionId = SceneSessionActionIds.SetActiveCamera,
                    NodeId = cam.Id.ToString(),
                });
                SceneRenderActions.SyncOpenPreviewFromActiveCamera();
            };
            _body.Children.Add(lookThrough);

            var match = new Button
            {
                Content = "Capture Viewport → Camera",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            ToolTip.SetTip(match, "Write the current main viewport view into this camera (eye, target, FOV).");
            match.Click += (_, _) => MatchViewport(cam);
            _body.Children.Add(match);

            _body.Children.Add(Label("Target"));
            _body.Children.Add(NumericRow("Tx", (decimal)cam.Target[0], v => SetTarget(0, (float)v)));
            _body.Children.Add(NumericRow("Ty", (decimal)cam.Target[1], v => SetTarget(1, (float)v)));
            _body.Children.Add(NumericRow("Tz", (decimal)cam.Target[2], v => SetTarget(2, (float)v)));

            void SetTarget(int axis, float value)
            {
                cam.Target[axis] = value;
                _session.Evaluator.NotifyNodeChanged(cam);
                _session.Execute(new AgentCommand
                {
                    ActionId = SceneSessionActionIds.Select,
                    NodeId = cam.Id.ToString(),
                });
            }
        }

        if (node is MeshNode mesh)
        {
            _body.Children.Add(Label($"Primitive: {mesh.Primitive}  size [{mesh.Size[0]:0.##},{mesh.Size[1]:0.##},{mesh.Size[2]:0.##}]"));
            var mats = _session.Document.Nodes.OfType<MaterialNode>().ToList();
            if (mats.Count > 0)
            {
                _body.Children.Add(Label("Material"));
                var combo = new ComboBox
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    ItemsSource = mats.Select(m => m.Name).ToList(),
                    SelectedItem = mats.FirstOrDefault(m => m.Id == mesh.MaterialId)?.Name,
                };
                combo.SelectionChanged += (_, _) =>
                {
                    if (combo.SelectedIndex < 0 || combo.SelectedIndex >= mats.Count)
                        return;
                    _session.Execute(new AgentCommand
                    {
                        ActionId = SceneSessionActionIds.SetMeshMaterial,
                        NodeId = mesh.Id.ToString(),
                        TargetId = mats[combo.SelectedIndex].Id.ToString(),
                    });
                };
                _body.Children.Add(combo);
            }

            _body.Children.Add(NumericRow("Seg", mesh.Segments, v =>
            {
                mesh.Segments = (int)v;
                _session.Evaluator.NotifyNodeChanged(mesh);
                _session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.Select, NodeId = mesh.Id.ToString() });
            }));
            _body.Children.Add(Chrome.Btn("Make Editable", () => _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.MakeEditable,
                NodeId = mesh.Id.ToString(),
            })));
        }

        _body.Children.Add(NumericRow("X", (decimal)node.Transform.Position[0], v => SetPos(0, (float)v)));
        _body.Children.Add(NumericRow("Y", (decimal)node.Transform.Position[1], v => SetPos(1, (float)v)));
        _body.Children.Add(NumericRow("Z", (decimal)node.Transform.Position[2], v => SetPos(2, (float)v)));

        void SetPos(int axis, float value)
        {
            var cmd = new AgentCommand
            {
                ActionId = SceneSessionActionIds.SetTransform,
                NodeId = node.Id.ToString(),
            };
            if (axis == 0) cmd.X = value;
            if (axis == 1) cmd.Y = value;
            if (axis == 2) cmd.Z = value;
            // preserve other axes
            cmd.X ??= node.Transform.Position[0];
            cmd.Y ??= node.Transform.Position[1];
            cmd.Z ??= node.Transform.Position[2];
            _session.Execute(cmd);
        }

        if (node is MaterialNode mat)
            _body.Children.Add(Label($"Color RGB ({mat.Color[0]:0.##},{mat.Color[1]:0.##},{mat.Color[2]:0.##})  rough {mat.Roughness:0.##}"));

        if (node is GeneratorNode gen)
        {
            _body.Children.Add(Label($"{gen.Generator} count={gen.Count} axis={gen.Axis}"));
            if (gen.Generator == GeneratorKind.Boole)
                _body.Children.Add(Label($"Boolean {gen.BooleanKind} target={gen.TargetId} cutter={gen.CutterId}"));
        }

        if (node is ModifierNode mod)
            _body.Children.Add(Label($"{mod.Modifier} tol={mod.Tolerance:0.####} levels={mod.Levels} dist={mod.Distance:0.##}"));
    }

    private Control IntensitySlider(LightNode light)
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 10,
            Value = light.Intensity,
            Width = 180,
        };
        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property != RangeBase.ValueProperty)
                return;
            _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.SetLight,
                NodeId = light.Id.ToString(),
                Intensity = (float)slider.Value,
            });
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Intensity", VerticalAlignment = VerticalAlignment.Center, Width = 70 },
                slider,
            },
        };
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
    };

    private static Control NumericRow(string label, decimal value, Action<decimal> onChanged)
    {
        var box = new NumericUpDown
        {
            Width = 100,
            Value = value,
            Increment = 0.1m,
            FormatString = "0.###",
        };
        box.ValueChanged += (_, _) =>
        {
            if (box.Value is { } v)
                onChanged(v);
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = label, Width = 40, VerticalAlignment = VerticalAlignment.Center },
                box,
            },
        };
    }
}
