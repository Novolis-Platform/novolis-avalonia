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
        Spacing = 6;
        Margin = new Thickness(8, 6);

        Children.Add(Btn("New", () => Cmd(SceneSessionActionIds.New)));
        Children.Add(Btn("Fit", () => Cmd(SceneSessionActionIds.Fit)));
        Children.Add(Sep());
        Children.Add(Btn("Omni", () => AddLight(LightKind.Omni)));
        Children.Add(Btn("Spot", () => AddLight(LightKind.Spot)));
        Children.Add(Btn("Infinite", () => AddLight(LightKind.Infinite)));
        Children.Add(Btn("Area", () => AddLight(LightKind.Area)));
        Children.Add(Sep());
        Children.Add(Btn("Camera", () => Cmd(SceneSessionActionIds.AddCamera)));
        Children.Add(Btn("Mesh", () => Cmd(SceneSessionActionIds.AddMesh)));
        Children.Add(Btn("Material", () => Cmd(SceneSessionActionIds.AddMaterial)));
        Children.Add(Sep());
        Children.Add(Btn("Cloner", () => Cmd(SceneSessionActionIds.AddGenerator, new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "cloner",
        })));
        Children.Add(Btn("Symmetry", () => Cmd(SceneSessionActionIds.AddGenerator, new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "symmetry",
        })));
        Children.Add(Btn("Weld", () => Cmd(SceneSessionActionIds.AddModifier, new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddModifier,
            ModifierKind = "weld",
        })));
        Children.Add(Sep());
        Children.Add(Btn("Delete", () => Cmd(SceneSessionActionIds.Delete)));
    }

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
            Padding = new Thickness(10, 4),
            Background = new SolidColorBrush(Color.FromRgb(32, 48, 62)),
            Foreground = Brushes.WhiteSmoke,
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
