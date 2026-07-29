using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>Selection mode strip that swaps with workspace.</summary>
public sealed class CadSelectionModeBar : StackPanel
{
    private CadWorkspace _workspace = CadWorkspace.Cad;
    private CadSelectionMode _mode = CadSelectionMode.Object;
    private readonly StackPanel _buttons = new() { Orientation = Orientation.Horizontal, Spacing = 2 };

    public CadSelectionModeBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Children.Add(new TextBlock
        {
            Text = "Select",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
            Opacity = 0.7,
        });
        Children.Add(_buttons);
        Rebuild();
    }

    public CadWorkspace Workspace
    {
        get => _workspace;
        set
        {
            if (_workspace == value)
                return;
            _workspace = value;
            _mode = CadSelectionMode.Object;
            Rebuild();
            SelectionModeChanged?.Invoke(_mode);
        }
    }

    public CadSelectionMode SelectionMode
    {
        get => _mode;
        set
        {
            if (_mode == value)
                return;
            _mode = value;
            Rebuild();
            SelectionModeChanged?.Invoke(_mode);
        }
    }

    public event Action<CadSelectionMode>? SelectionModeChanged;

    private void Rebuild()
    {
        _buttons.Children.Clear();
        foreach (var mode in ModesFor(_workspace))
        {
            var capture = mode;
            var btn = new Button
            {
                Content = Label(mode),
                Padding = new Thickness(8, 2),
                FontWeight = mode == _mode ? FontWeight.Bold : FontWeight.Normal,
            };
            btn.Click += (_, _) => SelectionMode = capture;
            _buttons.Children.Add(btn);
        }
    }

    private static IEnumerable<CadSelectionMode> ModesFor(CadWorkspace ws) =>
        ws switch
        {
            CadWorkspace.Modeling =>
            [
                CadSelectionMode.Object,
                CadSelectionMode.MeshIsland,
                CadSelectionMode.Face,
                CadSelectionMode.Edge,
                CadSelectionMode.Vertex,
            ],
            CadWorkspace.Preview =>
            [
                CadSelectionMode.Object,
                CadSelectionMode.MaterialSlot,
                CadSelectionMode.Light,
                CadSelectionMode.Camera,
            ],
            _ =>
            [
                CadSelectionMode.Object,
                CadSelectionMode.Body,
                CadSelectionMode.SketchElement,
            ],
        };

    private static string Label(CadSelectionMode mode) => mode switch
    {
        CadSelectionMode.Body => "Body",
        CadSelectionMode.SketchElement => "Sketch",
        CadSelectionMode.MeshIsland => "Island",
        CadSelectionMode.Face => "Face",
        CadSelectionMode.Edge => "Edge",
        CadSelectionMode.Vertex => "Vertex",
        CadSelectionMode.MaterialSlot => "Material",
        CadSelectionMode.Light => "Light",
        CadSelectionMode.Camera => "Camera",
        _ => "Object",
    };
}
