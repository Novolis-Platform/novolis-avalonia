using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;
using ScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility;

namespace Novolis.Avalonia.Git;

/// <summary>Lane commit graph + message list bound to <see cref="CommitGraphModel"/>.</summary>
public sealed class GitCommitGraphView : UserControl
{
    const double RowH = 28;
    const double LaneW = 16;
    const double Pad = 10;

    readonly Canvas _canvas = new() { Width = 120 };
    readonly ListBox _list = new() { SelectionMode = SelectionMode.Single };
    CommitGraphModel? _model;

    /// <summary>Commit selected.</summary>
    public event EventHandler<CommitSelectedEventArgs>? CommitSelected;

    /// <summary>Creates the view.</summary>
    public GitCommitGraphView()
    {
        _list.SelectionChanged += (_, _) =>
        {
            var node = (_list.SelectedItem as CommitRow)?.Node;
            CommitSelected?.Invoke(this, new CommitSelectedEventArgs(node));
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
        };
        var scrollCanvas = new ScrollViewer
        {
            Content = _canvas,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        Grid.SetColumn(scrollCanvas, 0);
        Grid.SetColumn(_list, 1);
        grid.Children.Add(scrollCanvas);
        grid.Children.Add(_list);
        Content = grid;
    }

    /// <summary>Binds graph model.</summary>
    public void SetGraph(CommitGraphModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        _list.ItemsSource = model.Nodes.Select(n => new CommitRow(n, TipLabels(n, model))).ToList();
        Paint(model);
    }

    static string TipLabels(CommitNode n, CommitGraphModel model)
    {
        var tips = model.TipRefs
            .Where(t => t.Sha is not null
                        && (n.Sha.StartsWith(t.Sha, StringComparison.OrdinalIgnoreCase)
                            || t.Sha.StartsWith(n.ShortSha, StringComparison.OrdinalIgnoreCase)))
            .Select(t => t.Name)
            .ToArray();
        return tips.Length == 0 ? "" : " " + string.Join(' ', tips.Select(t => $"[{t}]"));
    }

    void Paint(CommitGraphModel model)
    {
        _canvas.Children.Clear();
        var laneCount = Math.Max(1, model.Lanes.Count);
        _canvas.Width = Pad * 2 + laneCount * LaneW;
        _canvas.Height = Math.Max(RowH, model.Nodes.Count * RowH + Pad);

        var colors = new[]
        {
            Color.FromRgb(80, 180, 200),
            Color.FromRgb(220, 160, 80),
            Color.FromRgb(160, 120, 220),
            Color.FromRgb(100, 200, 140),
            Color.FromRgb(220, 100, 120),
            Color.FromRgb(120, 160, 220),
        };

        foreach (var edge in model.Edges)
        {
            var from = model.Nodes.FirstOrDefault(n => n.Sha == edge.From);
            var to = model.Nodes.FirstOrDefault(n => n.Sha == edge.To);
            if (from is null || to is null)
                continue;
            var x1 = Pad + from.Lane * LaneW + LaneW / 2;
            var y1 = Pad + from.Row * RowH + RowH / 2;
            var x2 = Pad + to.Lane * LaneW + LaneW / 2;
            var y2 = Pad + to.Row * RowH + RowH / 2;
            var brush = new SolidColorBrush(colors[from.Lane % colors.Length]);
            // Polyline approximates lane-change / merge curves without StreamGeometry API variance.
            var midY = (y1 + y2) / 2;
            _canvas.Children.Add(new Polyline
            {
                Points = new Points { new Point(x1, y1), new Point(x1, midY), new Point(x2, midY), new Point(x2, y2) },
                Stroke = brush,
                StrokeThickness = 1.5,
            });
        }

        foreach (var n in model.Nodes)
        {
            var cx = Pad + n.Lane * LaneW + LaneW / 2;
            var cy = Pad + n.Row * RowH + RowH / 2;
            var color = colors[n.Lane % colors.Length];
            _canvas.Children.Add(new Ellipse
            {
                Width = n.IsMerge ? 10 : 8,
                Height = n.IsMerge ? 10 : 8,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1,
                [Canvas.LeftProperty] = cx - (n.IsMerge ? 5 : 4),
                [Canvas.TopProperty] = cy - (n.IsMerge ? 5 : 4),
            });
        }
    }

    sealed class CommitRow
    {
        public CommitRow(CommitNode node, string tips)
        {
            Node = node;
            Tips = tips;
        }

        public CommitNode Node { get; }
        public string Tips { get; }
        public override string ToString() => $"{Node.ShortSha}  {Node.Subject}{Tips}";
    }
}
