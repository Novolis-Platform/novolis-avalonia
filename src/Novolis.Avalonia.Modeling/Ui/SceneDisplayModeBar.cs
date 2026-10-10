using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Modeling;

namespace Novolis.Avalonia.Modeling.Ui;

public sealed class SceneDisplayModeBar : StackPanel
{
    public SceneDisplayModeBar(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        foreach (SceneDisplayMode mode in Enum.GetValues<SceneDisplayMode>())
        {
            var m = mode;
            Children.Add(Chrome.Btn(Label(m), () => session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.SetDisplayMode,
                DisplayMode = m.ToString(),
            })));
        }
    }

    private static string Label(SceneDisplayMode m) => m switch
    {
        SceneDisplayMode.WirePoints => "Points",
        SceneDisplayMode.Isoline => "Isoline",
        _ => "Wire",
    };
}
