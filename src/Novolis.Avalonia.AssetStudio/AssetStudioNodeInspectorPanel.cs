using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.AssetStudio.Authoring;
using Novolis.Avalonia.GraphicalProfile;
using Novolis.Visual.Definitions;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Descriptor-driven node inspector backed by the same node-value command path
/// as the graph and source views.
/// </summary>
public sealed class AssetStudioNodeInspectorPanel : AssetStudioPanel
{
    private readonly StackPanel _content;
    private VisualNodeRegistry _registry = VisualNodeRegistry.CreateDefault();
    private NodeId? _nodeId;

    public AssetStudioNodeInspectorPanel()
        : base(
            "Node Inspector",
            CreateContent(out var content),
            "assetstudio.panel.node-inspector")
    {
        _content = content;
    }

    public event EventHandler<AssetStudioNodeValueChangedEventArgs>?
        ValueChanged;

    public NodeId? NodeId => _nodeId;

    public void SetNode(
        VisualRecipe? recipe,
        NodeId? nodeId,
        VisualNodeRegistry? registry = null)
    {
        _nodeId = nodeId;
        _registry = registry ?? VisualNodeRegistry.CreateDefault();
        _content.Children.Clear();
        if (recipe is null || nodeId is null)
        {
            _content.Children.Add(Muted("Select a graph node to edit its values."));
            return;
        }

        var fields = AssetStudioNodeInspector.Create(
            recipe,
            nodeId.Value,
            _registry);
        if (fields.Length == 0)
        {
            _content.Children.Add(
                Muted("This node has no descriptor-defined editable values."));
            return;
        }

        foreach (var field in fields)
        {
            if (field.Value is null)
            {
                continue;
            }

            var label = new TextBlock
            {
                Text = field.DisplayName,
                FontWeight = FontWeight.SemiBold,
            };
            ProfileBinding.Bind(
                label,
                TextBlock.ForegroundProperty,
                Profile.TextResourceKey);
            var editor = new TextBox
            {
                Text = FormatValue(field.Value),
            };
            ProfileBinding.Bind(
                editor,
                TextBox.BackgroundProperty,
                Profile.RaisedResourceKey);
            ProfileBinding.Bind(
                editor,
                TextBox.ForegroundProperty,
                Profile.TextResourceKey);
            ProfileBinding.Bind(
                editor,
                TextBox.BorderBrushProperty,
                Profile.BorderResourceKey);
            editor.SetValue(
                AutomationProperties.NameProperty,
                field.DisplayName);
            editor.SetValue(
                AutomationProperties.AutomationIdProperty,
                $"assetstudio.node.{nodeId}.value.{field.Name}");
            void Commit()
            {
                if (TryParseValue(field.Value, editor.Text, out var value))
                {
                    ValueChanged?.Invoke(
                        this,
                        new AssetStudioNodeValueChangedEventArgs(
                            nodeId.Value,
                            field.Name,
                            value));
                }
            }

            editor.KeyDown += (_, args) =>
            {
                if (args.Key == global::Avalonia.Input.Key.Enter)
                {
                    Commit();
                    args.Handled = true;
                }
            };
            editor.LostFocus += (_, _) => Commit();
            _content.Children.Add(
                new StackPanel
                {
                    Spacing = 3,
                    Margin = new Thickness(0, 3),
                    Children =
                    {
                        label,
                        new TextBlock
                        {
                            Text = field.Description ??
                                   field.Contract.ValueKind.ToString(),
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 10,
                        },
                        editor,
                    },
                });
        }
    }

    private static Control CreateContent(out StackPanel content)
    {
        content = new StackPanel { Spacing = 2 };
        content.SetValue(
            AutomationProperties.NameProperty,
            "Descriptor-driven node inspector");
        content.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.node.inspector");
        return new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
    }

    private static TextBlock Muted(string text)
    {
        var result = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
        };
        ProfileBinding.Bind(
            result,
            TextBlock.ForegroundProperty,
            Profile.MutedResourceKey);
        return result;
    }

    private static string FormatValue(VisualValue value) =>
        value switch
        {
            ScalarValue scalar => scalar.Value.ToString(
                "0.###",
                System.Globalization.CultureInfo.InvariantCulture),
            IntegerValue integer => integer.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue boolean => boolean.Value ? "true" : "false",
            TextValue text => text.Value,
            ColorValue color => string.Join(
                ",",
                color.Red.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                color.Green.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                color.Blue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                color.Alpha.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)),
            _ => value.Kind.ToString(),
        };

    private static bool TryParseValue(
        VisualValue template,
        string? text,
        out VisualValue value)
    {
        value = template;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var culture = System.Globalization.CultureInfo.InvariantCulture;
        switch (template)
        {
            case ScalarValue scalar when double.TryParse(
                text,
                System.Globalization.NumberStyles.Float,
                culture,
                out var number):
                value = new ScalarValue(number, scalar.Unit);
                return true;
            case IntegerValue integer when long.TryParse(
                text,
                System.Globalization.NumberStyles.Integer,
                culture,
                out var whole):
                value = new IntegerValue(whole, integer.Unit);
                return true;
            case BooleanValue when bool.TryParse(text, out var boolean):
                value = new BooleanValue(boolean);
                return true;
            case TextValue:
                value = new TextValue(text);
                return true;
            case ColorValue color:
                var parts = text.Split(
                    ',',
                    StringSplitOptions.TrimEntries |
                    StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length is < 3 or > 4 ||
                    parts.Any(
                        part => !double.TryParse(
                            part,
                            System.Globalization.NumberStyles.Float,
                            culture,
                            out _)))
                {
                    return false;
                }

                var values = parts.Select(
                        part => double.Parse(
                            part,
                            System.Globalization.NumberStyles.Float,
                            culture))
                    .ToArray();
                value = new ColorValue(
                    values[0],
                    values[1],
                    values[2],
                    values.Length == 4 ? values[3] : color.Alpha,
                    color.ColorSpace);
                return true;
            default:
                return false;
        }
    }
}
