using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Scene;
using Novolis.Avalonia.Cad.Session;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>Workspace-aware property editor; mutations go through session Execute.</summary>
public sealed class CadPropertyPanel : UserControl
{
    private readonly CadDocumentSession _document;
    private CadSessionService? _session;
    private readonly TextBlock _title = new() { FontWeight = FontWeight.SemiBold, FontSize = 14 };
    private readonly StackPanel _body = new() { Spacing = 6 };
    private CadWorkspace _workspace = CadWorkspace.Cad;

    public CadPropertyPanel(CadDocumentSession session)
    {
        _document = session;
        Content = new StackPanel
        {
            Margin = new Thickness(8),
            Spacing = 8,
            Children = { _title, _body },
        };
        _document.Changed += Refresh;
        Refresh();
    }

    public CadSessionService? SessionService
    {
        get => _session;
        set => _session = value;
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
        _body.Children.Clear();
        var entity = _document.SelectedEntity;
        if (entity is null)
        {
            _title.Text = CadWorkspaceMapping.ToDisplay(_workspace);
            _body.Children.Add(new TextBlock
            {
                Text = _workspace switch
                {
                    CadWorkspace.Modeling => "Select a mesh or Mesh From Solid adapter.\nModifiers: Weld, Optimize, Bridge.",
                    CadWorkspace.Preview => "Select geometry, material, light, or camera.",
                    _ => "Select an object, body, or sketch element.\nGenerators: Boolean, Symmetry, Array, Connect, Split.",
                },
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.85,
                FontSize = 12,
            });
            return;
        }

        var cat = CadSceneGraph.Classify(entity);
        _title.Text = entity.Name ?? entity.Kind;
        _body.Children.Add(Info(
            $"Kind: {entity.Kind}\nCategory: {cat}\nId: {entity.Id}" +
            (entity.ParentId is { } p ? $"\nParent: {p}" : "") +
            WorkspaceHint(entity, cat)));

        var materialBox = new TextBox { Text = entity.Material ?? "", PlaceholderText = "Material name" };
        var applyMat = new Button { Content = "Apply material", HorizontalAlignment = HorizontalAlignment.Stretch };
        applyMat.Click += (_, _) =>
        {
            _session?.Execute(new CadCommandDto
            {
                ActionId = CadSessionActionIds.SetMaterial,
                EntityId = entity.Id,
                Kind = materialBox.Text,
            });
            Refresh();
        };
        _body.Children.Add(new TextBlock { Text = "Material", FontSize = 11, Opacity = 0.7 });
        _body.Children.Add(materialBox);
        _body.Children.Add(applyMat);

        if (entity.Kind.Equals("wall", StringComparison.OrdinalIgnoreCase))
        {
            var sideA = new TextBox
            {
                Text = entity.Sides?.A?.ShapeId?.ToString() ?? "",
                PlaceholderText = "Side A shape Guid",
            };
            var sideB = new TextBox
            {
                Text = entity.Sides?.B?.ShapeId?.ToString() ?? "",
                PlaceholderText = "Side B shape Guid",
            };
            var applySides = new Button { Content = "Apply wall sides", HorizontalAlignment = HorizontalAlignment.Stretch };
            applySides.Click += (_, _) =>
            {
                if (_session is null)
                    return;
                if (!string.IsNullOrWhiteSpace(sideA.Text))
                {
                    _session.Execute(new CadCommandDto
                    {
                        ActionId = CadSessionActionIds.SetWallSide,
                        EntityId = entity.Id,
                        Properties = new Dictionary<string, string>
                        {
                            ["side"] = "A",
                            ["shapeId"] = sideA.Text.Trim(),
                        },
                    });
                }

                if (!string.IsNullOrWhiteSpace(sideB.Text))
                {
                    _session.Execute(new CadCommandDto
                    {
                        ActionId = CadSessionActionIds.SetWallSide,
                        EntityId = entity.Id,
                        Properties = new Dictionary<string, string>
                        {
                            ["side"] = "B",
                            ["shapeId"] = sideB.Text.Trim(),
                        },
                    });
                }

                Refresh();
            };
            _body.Children.Add(new TextBlock { Text = "Wall sides", FontSize = 11, Opacity = 0.7 });
            _body.Children.Add(sideA);
            _body.Children.Add(sideB);
            _body.Children.Add(applySides);
        }
    }

    private static TextBlock Info(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.85,
        FontSize = 12,
    };

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
