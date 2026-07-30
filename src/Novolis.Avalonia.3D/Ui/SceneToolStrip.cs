using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

public sealed class SceneToolStrip : StackPanel
{
    private readonly SceneSessionService _session;

    public SceneToolStrip(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 6);

        Children.Add(Btn("New", () => Cmd(SceneSessionActionIds.New)));
        Children.Add(Btn("Fit", () => Cmd(SceneSessionActionIds.Fit)));
        Children.Add(Sep());

        foreach (var kind in new[]
                 {
                     MeshPrimitiveKind.Box, MeshPrimitiveKind.Sphere, MeshPrimitiveKind.Cylinder,
                     MeshPrimitiveKind.Cone, MeshPrimitiveKind.Plane, MeshPrimitiveKind.Capsule,
                     MeshPrimitiveKind.Torus,
                 })
        {
            var k = kind;
            Children.Add(Btn(k.ToString(), () => AddMesh(k)));
        }

        Children.Add(Sep());
        Children.Add(Btn("Array", () => Cmd(SceneSessionActionIds.AddGenerator, new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "cloner",
            Count = 4,
        })));
        Children.Add(Btn("Symmetry", () => Cmd(SceneSessionActionIds.AddGenerator, new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "symmetry",
        })));
        Children.Add(Btn("Boolean", () => Cmd(SceneSessionActionIds.AddBoole, new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddBoole,
            BooleanKind = "difference",
        })));

        Children.Add(Sep());
        Children.Add(Btn("Extrude", () => AddMod(ModifierKind.Extrude)));
        Children.Add(Btn("Bevel", () => AddMod(ModifierKind.Bevel)));
        Children.Add(Btn("Weld", () => AddMod(ModifierKind.Weld)));
        Children.Add(Btn("Optimize", () => AddMod(ModifierKind.Optimize)));
        Children.Add(Btn("Subdiv", () => AddMod(ModifierKind.Subdivision)));

        Children.Add(Sep());
        Children.Add(Btn("Camera", () => Cmd(SceneSessionActionIds.AddCamera)));
        Children.Add(Btn("Material", () => Cmd(SceneSessionActionIds.AddMaterial)));

        Children.Add(Sep());
        Children.Add(Btn("Point", () => AddLight(LightKind.Omni)));
        Children.Add(Btn("Spot", () => AddLight(LightKind.Spot)));
        Children.Add(Btn("Directional", () => AddLight(LightKind.Infinite)));
        Children.Add(Btn("Area", () => AddLight(LightKind.Area)));

        Children.Add(Sep());
        Children.Add(Btn("Delete", () => Cmd(SceneSessionActionIds.Delete)));
    }

    private void AddMesh(MeshPrimitiveKind kind) =>
        _session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddMesh,
            Primitive = kind.ToString().ToLowerInvariant(),
            Name = kind.ToString(),
        });

    private void AddMod(ModifierKind kind) =>
        _session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddModifier,
            ModifierKind = kind.ToString().ToLowerInvariant(),
            Distance = kind is ModifierKind.Extrude or ModifierKind.Bevel ? 0.2f : null,
            Count = kind == ModifierKind.Subdivision ? 1 : null,
        });

    private void AddLight(LightKind kind) =>
        _session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddLight,
            LightKind = kind.ToString().ToLowerInvariant(),
        });

    private void Cmd(string actionId, AgentCommandDto? dto = null) =>
        _session.Execute(dto ?? new AgentCommandDto { ActionId = actionId });

    private static Button Btn(string label, Action onClick)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromRgb(32, 48, 62)),
            Foreground = Brushes.WhiteSmoke,
            FontSize = 12,
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    private static Border Sep() => new()
    {
        Width = 1,
        Background = new SolidColorBrush(Color.FromRgb(60, 80, 100)),
        Margin = new Thickness(4, 2),
    };
}
