using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Audio;

/// <summary>Magix-style edit tool modes for the arrangement timeline.</summary>
public enum ArrangementTool
{
    /// <summary>Select clips, scrub, time-range, trim handles.</summary>
    Select,

    /// <summary>Drag clips in time and across tracks.</summary>
    Move,

    /// <summary>Click a clip to split at the pointer (razor).</summary>
    Split,

    /// <summary>Click empty lane to place the selected library sound at playhead/time.</summary>
    Draw,

    /// <summary>Click a clip to delete it.</summary>
    Delete,
}

/// <summary>Visible Magix Music Maker–style tool strip above the arrangement.</summary>
public sealed class ArrangementToolBar : StackPanel
{
    ArrangementTool _tool = ArrangementTool.Select;
    readonly Dictionary<ArrangementTool, Button> _buttons = new();

    public ArrangementToolBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 6;
        Children.Add(Label("Tools"));
        AddTool(ArrangementTool.Select, "Select");
        AddTool(ArrangementTool.Move, "Move");
        AddTool(ArrangementTool.Split, "Split");
        AddTool(ArrangementTool.Draw, "Draw");
        AddTool(ArrangementTool.Delete, "Delete");
        RefreshStyles();
    }

    public ArrangementTool Tool
    {
        get => _tool;
        set
        {
            if (_tool == value)
                return;
            _tool = value;
            RefreshStyles();
            ToolChanged?.Invoke(value);
        }
    }

    public event Action<ArrangementTool>? ToolChanged;

    void AddTool(ArrangementTool tool, string label)
    {
        var b = new Button
        {
            Content = label,
            Padding = new Thickness(12, 6),
            Foreground = Brushes.White,
        };
        b.Click += (_, _) => Tool = tool;
        _buttons[tool] = b;
        Children.Add(b);
    }

    void RefreshStyles()
    {
        foreach (var (tool, button) in _buttons)
        {
            var on = tool == _tool;
            button.Background = on ? AudioEditPalette.Amber : AudioEditPalette.Accent;
            button.Foreground = on ? Brushes.Black : Brushes.White;
        }
    }

    static TextBlock Label(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Color.FromRgb(180, 190, 200)),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 4, 0),
        FontSize = 12,
    };
}
