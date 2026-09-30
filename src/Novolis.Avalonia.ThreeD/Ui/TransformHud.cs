using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

public sealed class TransformHud : UserControl
{
    private readonly SceneSessionService _session;
    private readonly NumericUpDown _x = Num();
    private readonly NumericUpDown _y = Num();
    private readonly NumericUpDown _z = Num();
    private bool _suppress;

    public TransformHud(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        var apply = Chrome.Btn("Apply Δ", Apply);
        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Margin = new Thickness(8, 4),
            Children =
            {
                new TextBlock { Text = "ΔX", VerticalAlignment = VerticalAlignment.Center },
                _x,
                new TextBlock { Text = "ΔY", VerticalAlignment = VerticalAlignment.Center },
                _y,
                new TextBlock { Text = "ΔZ", VerticalAlignment = VerticalAlignment.Center },
                _z,
                apply,
            },
        };
        _session.DocumentChanged += SyncFromSelection;
        SyncFromSelection();
    }

    private void SyncFromSelection()
    {
        _suppress = true;
        _x.Value = 0;
        _y.Value = 0;
        _z.Value = 0;
        _suppress = false;
    }

    private void Apply()
    {
        if (_suppress)
            return;
        _session.Execute(new AgentCommand
        {
            ActionId = SceneSessionActionIds.MoveSelection,
            X = (float)(_x.Value ?? 0),
            Y = (float)(_y.Value ?? 0),
            Z = (float)(_z.Value ?? 0),
        });
        SyncFromSelection();
    }

    private static NumericUpDown Num() => new()
    {
        Width = 72,
        Increment = 0.1m,
        FormatString = "0.###",
        Value = 0,
    };
}
