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
            _body.Children.Add(Label($"Primitive: {mesh.Primitive}  size [{mesh.Size[0]:0.##},{mesh.Size[1]:0.##},{mesh.Size[2]:0.##}]"));

        if (node is MaterialNode mat)
            _body.Children.Add(Label($"Color RGB ({mat.Color[0]:0.##},{mat.Color[1]:0.##},{mat.Color[2]:0.##})  rough {mat.Roughness:0.##}"));

        if (node is GeneratorNode gen)
            _body.Children.Add(Label($"{gen.Generator} count={gen.Count} axis={gen.Axis}"));

        if (node is ModifierNode mod)
            _body.Children.Add(Label($"{mod.Modifier} tol={mod.Tolerance:0.####} levels={mod.Levels}"));
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
}
