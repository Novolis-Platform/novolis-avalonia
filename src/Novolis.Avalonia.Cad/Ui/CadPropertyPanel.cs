using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Scene;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>Workspace-aware property readout for the current selection.</summary>
public sealed class CadPropertyPanel : UserControl
{
    private readonly CadDocumentSession _session;
    private readonly TextBlock _title = new() { FontWeight = FontWeight.SemiBold, FontSize = 14 };
    private readonly TextBlock _body = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.85,
        FontSize = 12,
    };
    private CadWorkspace _workspace = CadWorkspace.Cad;

    public CadPropertyPanel(CadDocumentSession session)
    {
        _session = session;
        Content = new StackPanel
        {
            Margin = new Thickness(8),
            Spacing = 8,
            Children = { _title, _body },
        };
        _session.Changed += Refresh;
        Refresh();
    }

    public CadWorkspace Workspace
    {
        get => _workspace;
        set
        {
            _workspace = value;
            Refresh();
        }
    }

    public void Refresh()
    {
        var entity = _session.SelectedEntity;
        if (entity is null)
        {
            _title.Text = CadWorkspaceMapping.ToDisplay(_workspace);
            _body.Text = _workspace switch
            {
                CadWorkspace.Modeling => "Select a mesh or Mesh From Solid adapter.\nModifiers: Weld, Optimize, Bridge.",
                CadWorkspace.Preview => "Select geometry, material, light, or camera.",
                _ => "Select an object, body, or sketch element.\nGenerators: Boolean, Symmetry, Array, Connect, Split.",
            };
            return;
        }

        var cat = CadSceneGraph.Classify(entity);
        _title.Text = entity.Name ?? entity.Kind;
        _body.Text =
            $"Kind: {entity.Kind}\n" +
            $"Category: {cat}\n" +
            $"Id: {entity.Id}\n" +
            (entity.ParentId is { } p ? $"Parent: {p}\n" : "") +
            (entity.OperandRole is { } r ? $"Role: {r}\n" : "") +
            (entity.SourceId is { } s ? $"Source: {s}\n" : "") +
            (entity.InputId is { } i ? $"Input: {i}\n" : "") +
            (entity.LinkMode is { } lm ? $"Link: {lm}\n" : "") +
            (entity.Material is { } mat ? $"Material: {mat}\n" : "") +
            (entity.Operation is { } op ? $"Operation: {op}\n" : "") +
            WorkspaceHint(entity, cat);
    }

    private string WorkspaceHint(CadEntity entity, CadSceneNodeCategory cat) =>
        _workspace switch
        {
            CadWorkspace.Modeling when cat is CadSceneNodeCategory.Geometry or CadSceneNodeCategory.Generator =>
                "\nCAD source — add Mesh From Solid to edit polygons.",
            CadWorkspace.Cad when cat is CadSceneNodeCategory.MeshModifier =>
                "\nMesh modifier — switch to Modeling to edit.",
            CadWorkspace.Preview when entity.Kind.Equals("light", StringComparison.OrdinalIgnoreCase) =>
                $"\nIntensity: {entity.Intensity}\nType: {entity.LightType ?? "point"}",
            _ => "",
        };
}
