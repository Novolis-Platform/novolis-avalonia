using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>
/// Two-row CAD chrome: file/modes on row 1; Object vs component tools on row 2.
/// Sections are wrapped in labeled group boxes.
/// </summary>
public sealed class SceneChromeShell : UserControl
{
    private readonly SceneSessionService _session;
    private readonly SceneEditorSurface _surface;
    private readonly Panel _contextRow;
    private readonly Control _primitivesGroup;
    private readonly Control _generatorsGroup;
    private readonly Control _lookGroup;
    private readonly Control _meshGroup;
    private SceneEditMode _lastMode = (SceneEditMode)(-1);

    public SceneChromeShell(SceneEditorSurface surface, string? dumpsDirectoryTooltip = null)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _session = surface.Session;

        _primitivesGroup = Chrome.Group("Primitives", surface.PrimitivePalette);
        _generatorsGroup = Chrome.Group("Generators", surface.GeneratorTools);
        _lookGroup = Chrome.Group("Look", surface.LookTools);
        _meshGroup = Chrome.Group("Mesh", surface.MeshEditTools);

        _contextRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Margin = new Thickness(2, 0, 2, 2),
        };

        var row1 = BuildRow(
            Chrome.Group("File", BuildFileCluster(dumpsDirectoryTooltip)),
            Chrome.Group("Edit", surface.EditModeBar),
            Chrome.Group("Display", surface.DisplayModeBar),
            Chrome.Group("Render", surface.RenderTools),
            Chrome.Group("Transform", surface.TransformHud));

        var row2 = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = _contextRow,
        };

        Content = new StackPanel
        {
            Spacing = 0,
            Children =
            {
                RowBorder(row1),
                RowBorder(row2),
            },
        };

        _session.DocumentChanged += RefreshContext;
        RefreshContext();
    }

    private Control BuildFileCluster(string? dumpsTooltip)
    {
        var dumpAll = Chrome.PrimaryBtn("Dump…", () =>
            SceneFileActions.Dump(this, _session, SceneSessionActionIds.Dump, Notice));
        ToolTip.SetTip(dumpAll,
            string.IsNullOrWhiteSpace(dumpsTooltip)
                ? "Choose a folder, then write viewport/window/scene/mesh artifacts there."
                : $"Choose a folder for dumps.\nDefault (HTTP without path): {dumpsTooltip}");

        var dumpMenu = new Menu
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
        };
        var dumpRoot = new MenuItem
        {
            Header = "▾",
            FontSize = 12,
            Foreground = Brushes.WhiteSmoke,
            Background = new SolidColorBrush(Color.FromRgb(28, 72, 78)),
            Padding = new Thickness(6, 4),
        };
        ToolTip.SetTip(dumpRoot, "Dump a single artifact type (folder picker).");

        void AddDump(string header, string actionId)
        {
            var item = new MenuItem { Header = header };
            item.Click += (_, _) =>
                SceneFileActions.Dump(this, _session, actionId, Notice);
            dumpRoot.Items.Add(item);
        }

        AddDump("All artifacts…", SceneSessionActionIds.DumpAll);
        AddDump("Viewport PNG…", SceneSessionActionIds.DumpViewport);
        AddDump("Window PNG…", SceneSessionActionIds.DumpWindow);
        AddDump("Scene JSON…", SceneSessionActionIds.DumpScene);
        AddDump("Mesh OBJ…", SceneSessionActionIds.DumpMesh);
        dumpMenu.Items.Add(dumpRoot);

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children =
            {
                Chrome.Btn("New", () => _session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.New })),
                Chrome.Btn("Open…", () => SceneFileActions.Open(this, _session, Notice)),
                Chrome.PrimaryBtn("Save", () => SceneFileActions.Save(this, _session, Notice)),
                Chrome.Btn("Save As…", () => SceneFileActions.SaveAs(this, _session, Notice)),
                Chrome.Btn("Import…", () => SceneFileActions.ImportMesh(this, _session, Notice)),
                dumpAll,
                dumpMenu,
                Chrome.PrimaryBtn("Fit", () =>
                {
                    _surface.Fit();
                    _session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.Fit });
                }),
                Chrome.Btn("Delete", () => _session.Execute(new AgentCommandDto { ActionId = SceneSessionActionIds.Delete })),
            },
        };
    }

    private void Notice(string message) => _surface.StatusBar.SetNotice(message);

    private void RefreshContext()
    {
        var mode = _session.Document.Edit.Mode;
        if (mode == _lastMode && _contextRow.Children.Count > 0)
            return;
        _lastMode = mode;

        _contextRow.Children.Clear();
        if (mode == SceneEditMode.Object)
        {
            _contextRow.Children.Add(_primitivesGroup);
            _contextRow.Children.Add(_generatorsGroup);
            _contextRow.Children.Add(_lookGroup);
        }
        else
        {
            _contextRow.Children.Add(_meshGroup);
        }
    }

    private static StackPanel BuildRow(params Control[] children)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            Margin = new Thickness(2, 0, 2, 0),
        };
        foreach (var child in children)
            row.Children.Add(child);
        return row;
    }

    private static Border RowBorder(Control child) => new()
    {
        BorderBrush = new SolidColorBrush(Color.FromRgb(40, 60, 75)),
        BorderThickness = new Thickness(0, 0, 0, 1),
        Child = child,
    };
}
