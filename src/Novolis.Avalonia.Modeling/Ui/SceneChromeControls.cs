using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.Modeling.Session;
using Novolis.Modeling;

namespace Novolis.Avalonia.Modeling.Ui;

public sealed class SceneEditModeBar : StackPanel
{
    private readonly SceneSessionService _session;

    public SceneEditModeBar(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        foreach (SceneEditMode mode in Enum.GetValues<SceneEditMode>())
        {
            var m = mode;
            Children.Add(Chrome.Btn(m.ToString(), () => _session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.SetEditMode,
                EditMode = m.ToString(),
            })));
        }

        Children.Add(Chrome.Sep());
        Children.Add(Chrome.Btn("Make Editable", () => _session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.MakeEditable,
        })));
    }
}
