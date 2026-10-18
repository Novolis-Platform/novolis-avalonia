using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Graph projection with a canvas-independent accessible node and port fallback.
/// </summary>
public sealed class AssetStudioGraphPanel : AssetStudioPanel
{
    private readonly TextBlock _summary;
    private readonly AssetStudioNodePortList _nodePorts;
    private readonly Canvas _canvas;
    private readonly Expander _accessibleNodes;
    private VisualNodeRegistry _registry = VisualNodeRegistry.CreateDefault();
    private readonly Dictionary<NodeId, Point> _nodePositions = [];
    private double _zoom = 1d;
    private Vector _pan;
    private Point? _panStart;
    private NodeId? _dragNodeId;
    private Point _dragStart;
    private Point _dragOrigin;

    public AssetStudioGraphPanel(
        VisualRecipe? recipe = null,
        VisualNodeRegistry? registry = null)
        : base(
            "Graph",
            CreateContent(
                out var summary,
                out var canvas,
                out var accessibleNodes,
                out var nodePorts),
            "assetstudio.panel.graph")
    {
        _summary = summary;
        _canvas = canvas;
        _accessibleNodes = accessibleNodes;
        _nodePorts = nodePorts;
        _nodePorts.EntryInvoked += OnAccessibleEntryInvoked;
        _nodePorts.NodeActionRequested += OnAccessibleNodeActionRequested;
        _canvas.PointerPressed += OnCanvasPointerPressed;
        _canvas.PointerMoved += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;
        _canvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        if (recipe is not null)
        {
            SetRecipe(recipe, registry);
        }
    }

    /// <summary>Accessible ordered node and port workflow.</summary>
    public AssetStudioNodePortList NodePorts => _nodePorts;

    /// <summary>Node currently selected by mouse or keyboard.</summary>
    public NodeId? SelectedNodeId { get; private set; }

    /// <summary>Raised when a node is selected or an action is requested.</summary>
    public event EventHandler<AssetStudioGraphNodeEventArgs>? NodeActionRequested;

    /// <summary>Raised when the graph asks the host to validate a new edge.</summary>
    public event EventHandler<AssetStudioGraphConnectionEventArgs>?
        ConnectionRequested;

    /// <summary>Recipe currently projected by the panel.</summary>
    public VisualRecipe? Recipe { get; private set; }

    /// <summary>Semantic-free navigation and layout state.</summary>
    public AssetStudioGraphViewState ViewState =>
        new(
            _zoom,
            _pan,
            SelectedOutputId,
            _nodePositions.ToImmutableDictionary());

    /// <summary>Output selected for inspection or preview.</summary>
    public string? SelectedOutputId { get; private set; }

    /// <summary>Replaces the semantic graph projection.</summary>
    public void SetRecipe(
        VisualRecipe recipe,
        VisualNodeRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        Recipe = recipe;
        _registry = registry ?? VisualNodeRegistry.CreateDefault();
        _nodePorts.SetRecipe(recipe, _registry);
        foreach (var nodeId in _nodePositions.Keys
                     .Where(nodeId => recipe.Nodes.All(node => node.Id != nodeId))
                     .ToArray())
        {
            _nodePositions.Remove(nodeId);
        }
        if (SelectedOutputId is not null &&
            recipe.Outputs.All(output => output.Id != SelectedOutputId))
        {
            SelectedOutputId = null;
        }
        _summary.Text =
            $"{recipe.Nodes.Length} node(s) · {recipe.Connections.Length} connection(s) · " +
            $"{recipe.Outputs.Length} output(s) · deterministic layout";
        if (SelectedNodeId is { } selected &&
            recipe.Nodes.All(node => node.Id != selected))
        {
            SelectedNodeId = null;
        }

        BuildGraph(recipe, _registry);
    }

    /// <summary>Restores view-only state without changing the recipe.</summary>
    public void SetViewState(AssetStudioGraphViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _zoom = ClampZoom(state.Zoom);
        _pan = state.Pan;
        SelectedOutputId = state.SelectedOutputId;
        _nodePositions.Clear();
        foreach (var pair in state.NodePositions)
        {
            _nodePositions[pair.Key] = pair.Value;
        }

        if (Recipe is { } recipe)
        {
            SetRecipe(recipe, _registry);
        }
    }

    /// <summary>Moves one node in the view without changing semantic state.</summary>
    public void MoveNode(NodeId nodeId, Vector delta)
    {
        if (Recipe?.Nodes.All(node => node.Id != nodeId) != false)
        {
            return;
        }

        var current = _nodePositions.TryGetValue(nodeId, out var position)
            ? position
            : new Point();
        _nodePositions[nodeId] = current + delta;
        if (Recipe is { } recipe)
        {
            BuildGraph(recipe, _registry);
        }
    }

    /// <summary>Changes graph zoom while preserving semantic fingerprints.</summary>
    public void SetZoom(double zoom)
    {
        _zoom = ClampZoom(zoom);
        ApplyViewTransform();
    }

    /// <summary>Moves the view camera without changing node semantics.</summary>
    public void PanBy(Vector delta)
    {
        _pan += delta;
        ApplyViewTransform();
    }

    /// <summary>Selects an output binding for inspection or preview.</summary>
    public void SelectOutput(string outputId)
    {
        if (Recipe?.Outputs.Any(output => output.Id == outputId) != true)
        {
            return;
        }

        SelectedOutputId = outputId;
        SelectNode(
            Recipe.Outputs.First(output => output.Id == outputId)
                .Source.NodeId);
    }

    /// <summary>Requests a typed connection; the host applies the shared command.</summary>
    public void RequestConnection(
        VisualPortReference source,
        VisualPortReference target) =>
        ConnectionRequested?.Invoke(
            this,
            new AssetStudioGraphConnectionEventArgs(source, target));

    /// <summary>Selects a node and updates the accessible list.</summary>
    public void SelectNode(NodeId nodeId)
    {
        if (Recipe?.Nodes.All(node => node.Id != nodeId) != false)
        {
            return;
        }

        SelectedNodeId = nodeId;
        var index = _nodePorts.Entries
            .Select((entry, index) => (entry, index))
            .FirstOrDefault(pair => !pair.entry.IsPort &&
                                    pair.entry.NodeId == nodeId)
            .index;
        if (_nodePorts.Entries.Count > 0)
        {
            _nodePorts.SelectedIndex = index;
        }

        NodeActionRequested?.Invoke(
            this,
            new AssetStudioGraphNodeEventArgs(nodeId));
    }

    /// <summary>Requests deletion of the current node.</summary>
    public void DeleteSelected()
    {
        if (SelectedNodeId is { } nodeId)
        {
            NodeActionRequested?.Invoke(
                this,
                new AssetStudioGraphNodeEventArgs(nodeId, "delete"));
        }
    }

    /// <summary>Requests duplication of the current node.</summary>
    public void DuplicateSelected()
    {
        if (SelectedNodeId is { } nodeId)
        {
            NodeActionRequested?.Invoke(
                this,
                new AssetStudioGraphNodeEventArgs(nodeId, "duplicate"));
        }
    }

    private static Control CreateContent(
        out TextBlock summary,
        out Canvas canvas,
        out Expander accessibleNodes,
        out AssetStudioNodePortList nodePorts)
    {
        summary = new TextBlock
        {
            Text = "No recipe loaded.",
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap,
        };
        ProfileBinding.Bind(
            summary,
            TextBlock.ForegroundProperty,
            Profile.MutedResourceKey);

        canvas = new Canvas
        {
            MinWidth = 720,
            MinHeight = 360,
        };
        ProfileBinding.Bind(
            canvas,
            Panel.BackgroundProperty,
            Profile.RaisedResourceKey);

        var canvasScroll = new ScrollViewer
        {
            Content = canvas,
            HorizontalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        nodePorts = new AssetStudioNodePortList();
        accessibleNodes = new Expander
        {
            Header = "Keyboard node and port order",
            IsExpanded = false,
            Content = new ScrollViewer
            {
                Height = 150,
                Content = nodePorts,
                HorizontalScrollBarVisibility =
                    global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            },
        };
        ProfileBinding.Bind(
            accessibleNodes,
            Expander.ForegroundProperty,
            Profile.TextResourceKey);

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            MinHeight = 420,
        };
        content.Children.Add(summary);
        Grid.SetRow(canvasScroll, 1);
        content.Children.Add(canvasScroll);
        Grid.SetRow(accessibleNodes, 2);
        content.Children.Add(accessibleNodes);
        return content;
    }

    private void BuildGraph(VisualRecipe recipe, VisualNodeRegistry registry)
    {
        _canvas.Children.Clear();
        if (recipe.Nodes.Length == 0)
        {
            _canvas.Width = 720;
            _canvas.Height = 360;
            return;
        }

        var nodes = recipe.Nodes.OrderBy(node => node.Id).ToArray();
        var descriptors = nodes.ToDictionary(
            node => node.Id,
            node => registry.TryGetDescriptor(node.TypeId, out var descriptor)
                ? descriptor
                : null);
        var layers = ComputeLayers(nodes, recipe);
        var layouts = new Dictionary<NodeId, GraphNodeLayout>();
        var outputNames = recipe.Outputs
            .GroupBy(output => output.Source.NodeId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(output => output.Name).ToArray());
        const double nodeWidth = 238;
        const double columnGap = 92;
        const double rowGap = 28;
        const double margin = 26;

        var maxX = 0d;
        var maxY = 0d;
        foreach (var layer in layers.OrderBy(pair => pair.Key))
        {
            var y = margin;
            foreach (var node in layer.Value)
            {
                var descriptor = descriptors[node.Id];
                var ports = descriptor?.Ports
                    .OrderBy(port => port.Direction)
                    .ThenBy(port => port.Id.Value, StringComparer.Ordinal)
                    .ToArray() ?? [];
                var height = global::System.Math.Max(
                    112,
                    70 + ports.Length * 23 + node.Values.Count * 18 +
                    (outputNames.TryGetValue(node.Id, out var names) ? names.Length * 18 : 0));
                var layout = new GraphNodeLayout(
                    node,
                    descriptor,
                    margin + layer.Key * (nodeWidth + columnGap),
                    y,
                    nodeWidth,
                    height,
                    ports);
                if (_nodePositions.TryGetValue(node.Id, out var position))
                {
                    layout = layout.WithPosition(position.X, position.Y);
                }
                layouts[node.Id] = layout;
                y += height + rowGap;
                maxX = global::System.Math.Max(maxX, layout.X + nodeWidth);
                maxY = global::System.Math.Max(maxY, layout.Y + height);
            }
        }

        _canvas.Width = maxX + margin;
        _canvas.Height = maxY + margin;
        ApplyViewTransform();

        foreach (var connection in recipe.Connections.OrderBy(connection => connection.Id))
        {
            if (!layouts.TryGetValue(connection.Source.NodeId, out var source) ||
                !layouts.TryGetValue(connection.Target.NodeId, out var target) ||
                !source.TryGetPort(connection.Source.PortId, PortDirection.Output, out var sourcePoint) ||
                !target.TryGetPort(connection.Target.PortId, PortDirection.Input, out var targetPoint))
            {
                continue;
            }

            var line = new Line
            {
                StartPoint = sourcePoint,
                EndPoint = targetPoint,
                StrokeThickness = 2,
                IsHitTestVisible = false,
            };
            ProfileBinding.Bind(
                line,
                Shape.StrokeProperty,
                Profile.AccentResourceKey);
            _canvas.Children.Insert(0, line);
        }

        var ordinal = 1;
        foreach (var node in nodes)
        {
            var layout = layouts[node.Id];
            var card = CreateNodeCard(
                layout,
                ordinal++,
                outputNames.TryGetValue(node.Id, out var names) ? names : []);
            card.PointerPressed += (_, args) =>
            {
                SelectNode(node.Id);
                if (args.GetCurrentPoint(card).Properties.PointerUpdateKind ==
                    PointerUpdateKind.LeftButtonPressed)
                {
                    _dragNodeId = node.Id;
                    _dragStart = args.GetPosition(_canvas);
                    _dragOrigin = new Point(layout.X, layout.Y);
                    args.Pointer.Capture(card);
                }
            };
            card.PointerMoved += (_, args) =>
            {
                if (_dragNodeId != node.Id)
                {
                    return;
                }

                var delta = args.GetPosition(_canvas) - _dragStart;
                Canvas.SetLeft(card, _dragOrigin.X + delta.X);
                Canvas.SetTop(card, _dragOrigin.Y + delta.Y);
            };
            card.PointerReleased += (_, args) =>
            {
                if (_dragNodeId != node.Id)
                {
                    return;
                }

                var delta = args.GetPosition(_canvas) - _dragStart;
                _nodePositions[node.Id] = _dragOrigin + delta;
                _dragNodeId = null;
                args.Pointer.Capture(null);
                BuildGraph(recipe, _registry);
            };
            Canvas.SetLeft(card, layout.X);
            Canvas.SetTop(card, layout.Y);
            _canvas.Children.Add(card);
        }
    }

    private void ApplyViewTransform()
    {
        _canvas.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(_zoom, _zoom),
                new TranslateTransform(_pan.X, _pan.Y),
            },
        };
    }

    private static double ClampZoom(double zoom) =>
        double.IsFinite(zoom)
            ? global::System.Math.Clamp(zoom, 0.25d, 3d)
            : 1d;

    private void OnAccessibleEntryInvoked(
        object? sender,
        EventArgs args)
    {
        if (_nodePorts.SelectedEntry is { } entry)
        {
            SelectNode(entry.NodeId);
            NodeActionRequested?.Invoke(
                this,
                new AssetStudioGraphNodeEventArgs(
                    entry.NodeId,
                    entry.IsPort ? "inspect-port" : "inspect"));
        }
    }

    private void OnAccessibleNodeActionRequested(
        object? sender,
        AssetStudioGraphNodeEventArgs args) =>
        NodeActionRequested?.Invoke(this, args);

    private void OnCanvasPointerPressed(
        object? sender,
        PointerPressedEventArgs args)
    {
        var point = args.GetCurrentPoint(_canvas);
        if (point.Properties.PointerUpdateKind ==
            PointerUpdateKind.RightButtonPressed)
        {
            DeleteSelected();
        }
        else if (point.Properties.PointerUpdateKind ==
                 PointerUpdateKind.MiddleButtonPressed)
        {
            _panStart = point.Position;
            args.Pointer.Capture(_canvas);
        }
    }

    private void OnCanvasPointerMoved(
        object? sender,
        PointerEventArgs args)
    {
        if (_panStart is not { } start)
        {
            return;
        }

        var current = args.GetCurrentPoint(_canvas).Position;
        PanBy(current - start);
        _panStart = current;
    }

    private void OnCanvasPointerReleased(
        object? sender,
        PointerReleasedEventArgs args)
    {
        if (_panStart is not null)
        {
            _panStart = null;
            args.Pointer.Capture(null);
        }
    }

    private void OnCanvasPointerWheelChanged(
        object? sender,
        PointerWheelEventArgs args)
    {
        SetZoom(_zoom + args.Delta.Y * 0.1d);
        args.Handled = true;
    }

    private static SortedDictionary<int, List<VisualNode>> ComputeLayers(
        IReadOnlyList<VisualNode> nodes,
        VisualRecipe recipe)
    {
        var nodeIds = nodes.Select(node => node.Id).ToHashSet();
        var incoming = nodes.ToDictionary(node => node.Id, _ => 0);
        var outgoing = nodes.ToDictionary(node => node.Id, _ => new List<NodeId>());
        foreach (var connection in recipe.Connections)
        {
            if (!nodeIds.Contains(connection.Source.NodeId) ||
                !nodeIds.Contains(connection.Target.NodeId))
            {
                continue;
            }

            incoming[connection.Target.NodeId]++;
            outgoing[connection.Source.NodeId].Add(connection.Target.NodeId);
        }

        var depth = nodes.ToDictionary(node => node.Id, _ => 0);
        var ready = incoming
            .Where(pair => pair.Value == 0)
            .Select(pair => pair.Key)
            .OrderBy(id => id)
            .ToList();
        var processed = new HashSet<NodeId>();
        while (ready.Count > 0)
        {
            var current = ready[0];
            ready.RemoveAt(0);
            if (!processed.Add(current))
            {
                continue;
            }

            foreach (var target in outgoing[current].OrderBy(id => id))
            {
                depth[target] = global::System.Math.Max(
                    depth[target],
                    depth[current] + 1);
                incoming[target]--;
                if (incoming[target] == 0)
                {
                    ready.Add(target);
                    ready.Sort();
                }
            }
        }

        var layers = new SortedDictionary<int, List<VisualNode>>();
        foreach (var node in nodes)
        {
            if (!processed.Contains(node.Id))
            {
                depth[node.Id] = 0;
            }

            if (!layers.TryGetValue(depth[node.Id], out var layer))
            {
                layer = [];
                layers.Add(depth[node.Id], layer);
            }

            layer.Add(node);
        }

        foreach (var layer in layers.Values)
        {
            layer.Sort((left, right) => left.Id.CompareTo(right.Id));
        }

        return layers;
    }

    private static Border CreateNodeCard(
        GraphNodeLayout layout,
        int ordinal,
        IReadOnlyList<string> outputNames)
    {
        var node = layout.Node;
        var title = layout.Descriptor?.DisplayName ?? $"{node.TypeId} (unknown)";
        var content = new StackPanel { Spacing = 3 };
        var heading = new TextBlock
        {
            Text = $"N{ordinal:00}  {title}",
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        ProfileBinding.Bind(heading, TextBlock.ForegroundProperty, Profile.TextResourceKey);
        content.Children.Add(heading);

        var type = new TextBlock
        {
            Text = node.TypeId,
            FontSize = 10,
            Opacity = 0.72,
        };
        ProfileBinding.Bind(type, TextBlock.ForegroundProperty, Profile.MutedResourceKey);
        content.Children.Add(type);

        foreach (var port in layout.Ports)
        {
            var direction = port.Direction == PortDirection.Input ? "←" : "→";
            var portText = new TextBlock
            {
                Text = $"{direction} {port.Id.Value} · {FormatContract(port.Contract)}",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
            };
            ProfileBinding.Bind(
                portText,
                TextBlock.ForegroundProperty,
                port.Direction == PortDirection.Input
                    ? Profile.MutedResourceKey
                    : Profile.AccentResourceKey);
            content.Children.Add(portText);
        }

        foreach (var value in node.Values)
        {
            var valueText = new TextBlock
            {
                Text = $"{value.Key} = {FormatValue(value.Value)}",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
            };
            ProfileBinding.Bind(valueText, TextBlock.ForegroundProperty, Profile.TextResourceKey);
            content.Children.Add(valueText);
        }

        if (outputNames.Count > 0)
        {
            var outputs = new TextBlock
            {
                Text = $"OUTPUT · {string.Join(", ", outputNames)}",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            };
            ProfileBinding.Bind(outputs, TextBlock.ForegroundProperty, Profile.AccentResourceKey);
            content.Children.Add(outputs);
        }

        var card = new Border
        {
            Width = layout.Width,
            Height = layout.Height,
            Padding = new Thickness(10, 8),
            CornerRadius = new CornerRadius(4),
            Child = content,
        };
        ProfileBinding.Bind(card, Border.BackgroundProperty, Profile.RaisedResourceKey);
        ProfileBinding.Bind(card, Border.BorderBrushProperty, Profile.BorderResourceKey);
        card.BorderThickness = new Thickness(1);
        ToolTip.SetTip(card, $"Node N{ordinal:00}\n{node.Id}\n{node.TypeId}");
        card.SetValue(
            global::Avalonia.Automation.AutomationProperties.NameProperty,
            $"Node N{ordinal:00}, {title}");
        return card;
    }

    private static string FormatContract(VisualTypeContract contract) =>
        contract.Unit == VisualUnit.Unitless
            ? contract.ValueKind.ToString()
            : $"{contract.ValueKind} · {contract.Unit}";

    private static string FormatValue(VisualValue value) =>
        value switch
        {
            ScalarValue scalar =>
                scalar.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            IntegerValue integer => integer.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue boolean => boolean.Value ? "true" : "false",
            ColorValue color =>
                $"rgba({color.Red:0.##}, {color.Green:0.##}, {color.Blue:0.##}, {color.Alpha:0.##})",
            VectorValue vector => $"({string.Join(", ", vector.Components.Select(
                component => component.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)))})",
            _ => value.Kind.ToString(),
        };

    private sealed class GraphNodeLayout
    {
        private readonly Dictionary<PortId, double> _inputY;
        private readonly Dictionary<PortId, double> _outputY;

        public GraphNodeLayout(
            VisualNode node,
            VisualNodeDescriptor? descriptor,
            double x,
            double y,
            double width,
            double height,
            IReadOnlyList<VisualPortDescriptor> ports)
        {
            Node = node;
            Descriptor = descriptor;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Ports = ports;
            _inputY = [];
            _outputY = [];
            for (var i = 0; i < ports.Count; i++)
            {
                var portY = y + 58 + i * 23;
                if (ports[i].Direction == PortDirection.Input)
                {
                    _inputY[ports[i].Id] = portY;
                }
                else
                {
                    _outputY[ports[i].Id] = portY;
                }
            }
        }

        public VisualNode Node { get; }

        public VisualNodeDescriptor? Descriptor { get; }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }

        public IReadOnlyList<VisualPortDescriptor> Ports { get; }

        public GraphNodeLayout WithPosition(double x, double y) =>
            new(Node, Descriptor, x, y, Width, Height, Ports);

        public bool TryGetPort(
            PortId portId,
            PortDirection direction,
            out Point point)
        {
            var positions = direction == PortDirection.Input ? _inputY : _outputY;
            if (positions.TryGetValue(portId, out var y))
            {
                point = new Point(
                    direction == PortDirection.Input ? X : X + Width,
                    y);
                return true;
            }

            point = default;
            return false;
        }
    }
}
