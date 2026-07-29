using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

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
            var setActive = new Button { Content = "Set Active Camera", HorizontalAlignment = HorizontalAlignment.Stretch };
            setActive.Click += (_, _) => _session.Execute(new AgentCommandDto
            {
                ActionId = SceneSessionActionIds.SetActiveCamera,
                NodeId = cam.Id.ToString(),
            });
            _body.Children.Add(setActive);
        }

        if (node is MeshNode mesh)
        {
            _body.Children.Add(Label($"Primitive: {mesh.Primitive}  size [{mesh.Size[0]:0.##},{mesh.Size[1]:0.##},{mesh.Size[2]:0.##}]"));
            _body.Children.Add(NumericRow("Seg", mesh.Segments, v =>
            {
                mesh.Segments = (int)v;
                _session.Evaluator.NotifyNodeChanged(mesh);
                // force refresh via fake select
                _session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.Select, NodeId = mesh.Id.ToString() });
            }));
            _body.Children.Add(Chrome.Btn("Make Editable", () => _session.Execute(new AgentCommandDto
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
            var cmd = new AgentCommandDto
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
                _body.Children.Add(Label($"Boole {gen.BooleanKind} target={gen.TargetId} cutter={gen.CutterId}"));
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
            _session.Execute(new AgentCommandDto
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
