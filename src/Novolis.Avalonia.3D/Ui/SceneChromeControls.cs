using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

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
            Children.Add(Chrome.Btn(m.ToString(), () => _session.Execute(new AgentCommandDto
            {
                ActionId = SceneSessionActionIds.SetEditMode,
                EditMode = m.ToString(),
            })));
        }

        Children.Add(Chrome.Sep());
        Children.Add(Chrome.Btn("Make Editable", () => _session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.MakeEditable,
        })));
    }
}

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
            Children.Add(Chrome.Btn(Label(m), () => session.Execute(new AgentCommandDto
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

public sealed class PrimitivePalette : WrapPanel
{
    public PrimitivePalette(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Margin = new Thickness(8, 4);
        foreach (MeshPrimitiveKind kind in Enum.GetValues<MeshPrimitiveKind>())
        {
            var k = kind;
            Children.Add(Chrome.Btn(Short(k), () => session.Execute(new AgentCommandDto
            {
                ActionId = SceneSessionActionIds.AddMesh,
                Primitive = k.ToString().ToLowerInvariant(),
                Name = k.ToString(),
                Segments = k is MeshPrimitiveKind.Landscape ? 16 : 16,
            })));
        }
    }

    private static string Short(MeshPrimitiveKind k) => k switch
    {
        MeshPrimitiveKind.PlatonicTetra => "Tetra",
        MeshPrimitiveKind.PlatonicOcta => "Octa",
        MeshPrimitiveKind.PlatonicIcosa => "Icosa",
        MeshPrimitiveKind.PlatonicDodeca => "Dodeca",
        _ => k.ToString(),
    };
}

public sealed class GeneratorToolStrip : StackPanel
{
    public GeneratorToolStrip(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        Children.Add(Chrome.Btn("Array", () => session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "cloner",
            Count = 4,
        })));
        Children.Add(Chrome.Btn("Symmetry", () => session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddGenerator,
            GeneratorKind = "symmetry",
        })));
        Children.Add(Chrome.Btn("Boole", () => session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddBoole,
            BooleanKind = "difference",
        })));
    }
}

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
            Children.Add(Chrome.Btn(Short(k), () => session.Execute(new AgentCommandDto
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

public sealed class LookToolStrip : StackPanel
{
    public LookToolStrip(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Margin = new Thickness(8, 4);
        Children.Add(Chrome.Btn("Camera", () => session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.AddCamera })));
        Children.Add(Chrome.Btn("Material", () => session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.AddMaterial })));
        Children.Add(Chrome.Btn("Omni", () => AddLight(session, LightKind.Omni)));
        Children.Add(Chrome.Btn("Spot", () => AddLight(session, LightKind.Spot)));
        Children.Add(Chrome.Btn("Infinite", () => AddLight(session, LightKind.Infinite)));
        Children.Add(Chrome.Btn("Area", () => AddLight(session, LightKind.Area)));
        Children.Add(Chrome.Sep());
        Children.Add(Chrome.Btn("New", () => session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.New })));
        Children.Add(Chrome.Btn("Fit", () => session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.Fit })));
        Children.Add(Chrome.Btn("Delete", () => session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.Delete })));
    }

    private static void AddLight(SceneSessionService session, LightKind kind) =>
        session.Execute(new AgentCommandDto
        {
            ActionId = SceneSessionActionIds.AddLight,
            LightKind = kind.ToString().ToLowerInvariant(),
        });
}

public sealed class MeshAttributePanel : UserControl
{
    private readonly SceneSessionService _session;
    private readonly StackPanel _body = new() { Margin = new Thickness(8), Spacing = 4 };

    public MeshAttributePanel(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Content = new DockPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Mesh Attributes",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 8, 8, 4),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                new ScrollViewer { Content = _body },
            },
        };
        _session.DocumentChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        _body.Children.Clear();
        var edit = _session.Document.Edit;
        _body.Children.Add(Chrome.Label($"Mode: {edit.Mode}  Display: {edit.DisplayMode}"));
        _body.Children.Add(Chrome.Label($"Component selection: {edit.SelectionCount}"));

        var id = edit.EditMeshId ?? _session.Document.SelectionId;
        if (id is null || _session.Document.Find(id.Value) is not MeshNode mesh)
        {
            _body.Children.Add(Chrome.Label("No mesh selected."));
            return;
        }

        var editable = MeshEditBake.ReadBakedOrTessellate(mesh);
        _body.Children.Add(Chrome.Label($"Node: {mesh.Name}"));
        _body.Children.Add(Chrome.Label($"Verts: {editable.VertexCount}  Faces: {editable.TriangleCount}"));
        _body.Children.Add(Chrome.Label($"Primitive: {mesh.Primitive}  segments: {mesh.Segments}"));
        _body.Children.Add(Chrome.Label(mesh.Vertices is { Length: > 0 } ? "State: Editable (baked)" : "State: Procedural"));
    }
}

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
        _session.Execute(new AgentCommandDto
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

public sealed class ModifierStackPanel : UserControl
{
    private readonly SceneSessionService _session;
    private readonly StackPanel _body = new() { Margin = new Thickness(8), Spacing = 4 };

    public ModifierStackPanel(SceneSessionService session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        Content = new DockPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = "Modifier Stack",
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(8, 8, 8, 4),
                    [DockPanel.DockProperty] = Dock.Top,
                },
                new ScrollViewer { Content = _body },
            },
        };
        _session.DocumentChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        _body.Children.Clear();
        var id = _session.Document.SelectionId;
        if (id is null)
        {
            _body.Children.Add(Chrome.Label("Select a mesh."));
            return;
        }

        var mods = _session.Document.Nodes.OfType<ModifierNode>()
            .Where(m => m.InputId == id)
            .ToList();
        if (mods.Count == 0)
        {
            _body.Children.Add(Chrome.Label("No modifiers on selection."));
            return;
        }

        foreach (var mod in mods)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            row.Children.Add(Chrome.Label($"{mod.Name} ({mod.Modifier})"));
            var del = Chrome.Btn("×", () =>
            {
                _session.Document.SelectionId = mod.Id;
                _session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.Delete });
            });
            row.Children.Add(del);
            _body.Children.Add(row);
        }
    }
}

public sealed class ViewportStatusBar : UserControl
{
    private readonly TextBlock _text = new()
    {
        Margin = new Thickness(10, 4),
        FontSize = 12,
        Opacity = 0.9,
        Foreground = Brushes.WhiteSmoke,
    };

    public ViewportStatusBar(SceneSessionService session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Content = _text;
        session.DocumentChanged += () => Refresh(session);
        Refresh(session);
    }

    public void Refresh(SceneSessionService session)
    {
        var edit = session.Document.Edit;
        _text.Text =
            $"{session.Document.Name} · {edit.Mode} · {edit.DisplayMode} · components={edit.SelectionCount} · nodes={session.Document.Nodes.Count}";
    }
}

internal static class Chrome
{
    public static Button Btn(string label, Action onClick)
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

    public static Border Sep() => new()
    {
        Width = 1,
        Background = new SolidColorBrush(Color.FromRgb(60, 80, 100)),
        Margin = new Thickness(4, 2),
    };

    public static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brushes.WhiteSmoke,
    };
}
