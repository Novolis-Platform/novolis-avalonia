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
        var isOverride = _instance?.Overrides.ContainsKey(parameter.Id) == true;
        var label = new TextBlock
        {
            Text = parameter.Editor.DisplayName ?? parameter.Name,
            FontWeight = FontWeight.SemiBold,
        };
        ToolTip.SetTip(
            label,
            parameter.Editor.Description ?? parameter.Name);
        ProfileBinding.Bind(
            label,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey);

        var state = new TextBlock
        {
            Text = isOverride ? "OVERRIDE" : "INHERITED",
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        ProfileBinding.Bind(
            state,
            TextBlock.ForegroundProperty,
            isOverride
                ? Profile.AccentResourceKey
                : Profile.MutedResourceKey);
        var heading = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { label, state },
        };
        Grid.SetColumn(state, 1);

        var description = new TextBlock
        {
            Text = parameter.Editor.Description ??
                   $"{parameter.Contract.ValueKind} control",
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
        };
        ProfileBinding.Bind(
            description,
            TextBlock.ForegroundProperty,
            Profile.MutedResourceKey);

        var editor = CreateValueEditor(parameter, resolved);
        var reset = new Button
        {
            Content = "Reset",
            Padding = new Thickness(6, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsEnabled = isOverride,
        };
        ProfileBinding.Bind(
            reset,
            Button.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            reset,
            Button.ForegroundProperty,
            Profile.TextResourceKey);
        ProfileBinding.Bind(
            reset,
            Button.BorderBrushProperty,
            Profile.BorderResourceKey);
        reset.Click += (_, _) => ResetRequested?.Invoke(this, parameter.Id);

        var editorRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { editor, reset },
        };
        Grid.SetColumn(reset, 1);
        var card = new Border
        {
            Padding = new Thickness(8),
            Margin = new Thickness(0, 2),
            Child = new StackPanel
            {
                Spacing = 4,
                Children = { heading, description, editorRow },
            },
        };
        ProfileBinding.Bind(
            card,
            Border.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            card,
            Border.BorderBrushProperty,
            Profile.BorderResourceKey);
        card.BorderThickness = new Thickness(1);
        return card;
    }

    private Control CreateValueEditor(
        MaterialParameterDefinition parameter,
        VisualValue resolved)
    {
        if (resolved is ScalarValue scalar &&
            parameter.Editor.Control is
                VisualParameterControlKind.Slider or
                VisualParameterControlKind.Automatic)
        {
            var minimum = parameter.Editor.Minimum ??
                          Math.Min(0d, scalar.Value);
            var maximum = parameter.Editor.Maximum ??
                          Math.Max(1d, scalar.Value);
            if (maximum <= minimum)
            {
                maximum = minimum + 1d;
            }

            var slider = new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Clamp(scalar.Value, minimum, maximum),
                TickFrequency = parameter.Editor.Step ?? 0.01,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slider.SetValue(
                AutomationProperties.NameProperty,
                parameter.Editor.DisplayName ?? parameter.Name);
            slider.SetValue(
                AutomationProperties.AutomationIdProperty,
                $"assetstudio.material.parameter.{parameter.Id}.slider");

            var number = CreateTextEditor(
                parameter,
                resolved,
                FormatValue(resolved));
            void CommitSlider()
            {
                ParameterChanged?.Invoke(
                    this,
                    new AssetStudioMaterialParameterChangedEventArgs(
                        parameter.Id,
                        new ScalarValue(slider.Value, scalar.Unit)));
            }

            slider.PointerReleased += (_, _) => CommitSlider();
            slider.KeyUp += (_, args) =>
            {
                if (args.Key is
                    global::Avalonia.Input.Key.Left or
                    global::Avalonia.Input.Key.Right or
                    global::Avalonia.Input.Key.Up or
                    global::Avalonia.Input.Key.Down or
                    global::Avalonia.Input.Key.Enter)
                {
                    CommitSlider();
                }
            };
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children = { slider, number },
            };
            Grid.SetColumn(number, 1);
            return grid;
        }

        if (resolved is BooleanValue boolean)
        {
            var toggle = new CheckBox
            {
                IsChecked = boolean.Value,
                Content = boolean.Value ? "Enabled" : "Disabled",
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            toggle.SetValue(
                AutomationProperties.NameProperty,
                parameter.Editor.DisplayName ?? parameter.Name);
            toggle.SetValue(
                AutomationProperties.AutomationIdProperty,
                $"assetstudio.material.parameter.{parameter.Id}");
            void CommitToggle(bool value)
            {
                toggle.Content = value ? "Enabled" : "Disabled";
                ParameterChanged?.Invoke(
                    this,
                    new AssetStudioMaterialParameterChangedEventArgs(
                        parameter.Id,
                        new BooleanValue(value)));
            }

            toggle.IsCheckedChanged += (_, _) =>
                CommitToggle(toggle.IsChecked == true);
            return toggle;
        }

        if (resolved is ColorValue color)
        {
            var swatch = new Border
            {
                Width = 32,
                Height = 26,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(ToAvaloniaColor(color)),
                VerticalAlignment = VerticalAlignment.Center,
            };
            ToolTip.SetTip(
                swatch,
                "Color values use red,green,blue,alpha from 0 to 1.");
            var text = CreateTextEditor(
                parameter,
                resolved,
                FormatValue(resolved));
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 8,
                Children = { swatch, text },
            };
            Grid.SetColumn(text, 1);
            return grid;
        }

        return CreateTextEditor(parameter, resolved, FormatValue(resolved));
    }

    private TextBox CreateTextEditor(
        MaterialParameterDefinition parameter,
        VisualValue template,
        string text)
    {
        var value = new TextBox
        {
            Text = text,
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
        void Commit()
        {
            if (TryParseValue(template, value.Text, out var parsed))
            {
                ParameterChanged?.Invoke(
                    this,
                    new AssetStudioMaterialParameterChangedEventArgs(
                        parameter.Id,
                        parsed));
            }
        }

        value.KeyDown += (_, args) =>
        {
            if (args.Key == global::Avalonia.Input.Key.Enter)
            {
                Commit();
                args.Handled = true;
            }
        };
        value.LostFocus += (_, _) => Commit();
        return value;
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

    private static global::Avalonia.Media.Color ToAvaloniaColor(
        ColorValue color) =>
        global::Avalonia.Media.Color.FromArgb(
            (byte)Math.Clamp((int)Math.Round(color.Alpha * 255d), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Red * 255d), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Green * 255d), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Blue * 255d), 0, 255));

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
