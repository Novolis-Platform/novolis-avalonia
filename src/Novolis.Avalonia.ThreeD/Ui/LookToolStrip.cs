using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

public sealed class LookToolStrip : StackPanel
{
    public LookToolStrip(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        Children.Add(Chrome.Btn("Camera", () => session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.AddCamera })));
        Children.Add(Chrome.Btn("Material", () => session.Execute(new AgentCommand { ActionId = SceneSessionActionIds.AddMaterial })));
        Children.Add(Chrome.Btn("Point", () => AddLight(session, LightKind.Omni)));
        Children.Add(Chrome.Btn("Spot", () => AddLight(session, LightKind.Spot)));
        Children.Add(Chrome.Btn("Directional", () => AddLight(session, LightKind.Infinite)));
        Children.Add(Chrome.Btn("Area", () => AddLight(session, LightKind.Area)));
    }

    private static void AddLight(SceneSessionService session, LightKind kind) =>
        session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddLight,
            LightKind = kind.ToString().ToLowerInvariant(),
        });
}
