using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

public sealed class MeshEditToolStrip : StackPanel
{
    public MeshEditToolStrip(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        foreach (var kind in new[]
                 {
                     ModifierKind.Extrude, ModifierKind.Inset, ModifierKind.Bevel, ModifierKind.Bridge,
                     ModifierKind.Dissolve, ModifierKind.Knife, ModifierKind.Weld, ModifierKind.Optimize,
                     ModifierKind.Subdivision,
                 })
        {
            var k = kind;
            Children.Add(Chrome.Btn(Short(k), () => session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.MeshEdit,
                ModifierKind = k.ToString().ToLowerInvariant(),
                Distance = k is ModifierKind.Extrude or ModifierKind.Bevel or ModifierKind.Inset ? 0.2f : null,
                Count = k == ModifierKind.Subdivision ? 1 : null,
            })));
        }
    }

    private static string Short(ModifierKind k) => k switch
    {
        ModifierKind.Subdivision => "Subdiv",
        _ => k.ToString(),
    };
}
