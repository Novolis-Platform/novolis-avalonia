using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Modeling;

namespace Novolis.Avalonia.Modeling.Ui;

public sealed class GeneratorToolStrip : StackPanel
{
    public GeneratorToolStrip(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        Children.Add(Chrome.Btn("Array", () => session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "cloner",
            Count = 4,
        })));
        Children.Add(Chrome.Btn("Symmetry", () => session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "symmetry",
        })));
        Children.Add(Chrome.Btn("Boolean", () => session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.AddBoole,
            BooleanKind = "difference",
        })));
    }
}
