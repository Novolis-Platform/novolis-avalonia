using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Visual.Definitions;
using Novolis.Visual.Materials;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Friendly grouped control surface for material graphs and instances.
/// </summary>
public sealed class AssetStudioMaterialInspectorPanel : AssetStudioPanel
{
    private readonly StackPanel _content;
    private MaterialGraph? _graph;
    private MaterialInstance? _instance;

    public AssetStudioMaterialInspectorPanel()
        : base(
            "Material Inspector",
            CreateContent(out var content),
            "assetstudio.panel.material-inspector")
    {
        _content = content;
    }

    public event EventHandler<AssetStudioMaterialParameterChangedEventArgs>?
        ParameterChanged;

    public event EventHandler<string>? ResetRequested;

    public MaterialGraph? Graph => _graph;

    public MaterialInstance? Instance => _instance;

    public void SetMaterial(
        MaterialGraph? graph,
        MaterialInstance? instance = null)
    {
        _graph = graph;
        _instance = graph is null
            ? null
            : instance ?? new MaterialInstance(
                graph.MaterialId + ".instance",
                graph);
        _content.Children.Clear();
        if (_graph is null)
        {
            _content.Children.Add(
                CreateMutedText("Select a material or template to edit its controls."));
            return;
        }

        var parameters = _graph.Parameters;
        if (parameters.Length == 0)
        {
            _content.Children.Add(
                CreateMutedText("No exposed controls. Promote a node value to make it reusable."));
            return;
        }

        foreach (var group in parameters
                     .GroupBy(
                         parameter => parameter.Editor.Group ?? "Material",
                         StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var groupTitle = new TextBlock
            {
                Text = group.Key,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 8, 0, 4),
            };
            ProfileBinding.Bind(
                groupTitle,
                TextBlock.ForegroundProperty,
                Profile.AccentResourceKey);
            _content.Children.Add(groupTitle);
            foreach (var parameter in group)
            {
                _content.Children.Add(CreateEditor(parameter));
            }
        }
    }

    private Control CreateEditor(MaterialParameterDefinition parameter)
    {
        var resolved = _instance?.Resolve(parameter.Id) ?? parameter.DefaultValue;
        var label = new TextBlock
        {
            Text = parameter.Editor.DisplayName ?? parameter.Name,
        };
        ToolTip.SetTip(
            label,
            parameter.Editor.Description ?? parameter.Name);
        ProfileBinding.Bind(
            label,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey);
        var value = new TextBox
        {
            Text = FormatValue(resolved),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        ProfileBinding.Bind(
            value,
            TextBox.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            value,
            TextBox.ForegroundProperty,
            Profile.TextResourceKey);
        ProfileBinding.Bind(
            value,
            TextBox.BorderBrushProperty,
            Profile.BorderResourceKey);
        value.SetValue(
            AutomationProperties.NameProperty,
            parameter.Editor.DisplayName ?? parameter.Name);
        value.SetValue(
            AutomationProperties.AutomationIdProperty,
            $"assetstudio.material.parameter.{parameter.Id}");
        value.KeyDown += (_, args) =>
        {
            if (args.Key != global::Avalonia.Input.Key.Enter ||
                !TryParseValue(parameter.DefaultValue, value.Text, out var parsed))
            {
                return;
            }

            ParameterChanged?.Invoke(
                this,
                new AssetStudioMaterialParameterChangedEventArgs(
                    parameter.Id,
                    parsed));
            args.Handled = true;
        };
        value.LostFocus += (_, _) =>
        {
            if (TryParseValue(
                    parameter.DefaultValue,
                    value.Text,
                    out var parsed))
            {
                ParameterChanged?.Invoke(
                    this,
                    new AssetStudioMaterialParameterChangedEventArgs(
                        parameter.Id,
                        parsed));
            }
        };

        var reset = new Button
        {
            Content = "Reset",
            Padding = new Thickness(6, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        ProfileBinding.Bind(
            reset,
            Button.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            reset,
            Button.ForegroundProperty,
            Profile.TextResourceKey);
        reset.Click += (_, _) => ResetRequested?.Invoke(this, parameter.Id);
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 2),
            Children =
            {
                label,
                value,
                reset,
            },
        };
        Grid.SetColumnSpan(label, 2);
        Grid.SetRow(value, 1);
        Grid.SetRow(reset, 1);
        Grid.SetColumn(reset, 1);
        return grid;
    }

    private static Control CreateContent(out StackPanel content)
    {
        content = new StackPanel
        {
            Spacing = 2,
        };
        content.SetValue(
            AutomationProperties.NameProperty,
            "Material exposed parameter inspector");
        content.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.material.inspector");
        return new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
    }

    private static TextBlock CreateMutedText(string text)
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
            ScalarValue scalar =>
                scalar.Value.ToString(
                    "0.###",
                    System.Globalization.CultureInfo.InvariantCulture),
            IntegerValue integer => integer.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue boolean => boolean.Value ? "true" : "false",
            TextValue text => text.Value,
            ColorValue color =>
                string.Join(
                    ",",
                    color.Red.ToString(
                        "0.###",
                        System.Globalization.CultureInfo.InvariantCulture),
                    color.Green.ToString(
                        "0.###",
                        System.Globalization.CultureInfo.InvariantCulture),
                    color.Blue.ToString(
                        "0.###",
                        System.Globalization.CultureInfo.InvariantCulture),
                    color.Alpha.ToString(
                        "0.###",
                        System.Globalization.CultureInfo.InvariantCulture)),
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

                var values = parts
                    .Select(
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
