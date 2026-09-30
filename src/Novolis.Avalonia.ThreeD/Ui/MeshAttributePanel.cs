using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia.ThreeD.Session;
using Novolis.ThreeD;

namespace Novolis.Avalonia.ThreeD.Ui;

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
