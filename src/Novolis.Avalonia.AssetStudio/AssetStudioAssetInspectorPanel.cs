using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Visual.Assets;
using Novolis.Visual.Definitions;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Friendly grouped control surface for assembled asset instances.
/// </summary>
public sealed class AssetStudioAssetInspectorPanel : AssetStudioPanel
{
    private readonly StackPanel _content;
    private AssetRecipe? _asset;
    private AssetInstance? _instance;

    public AssetStudioAssetInspectorPanel()
        : base(
            "Asset Inspector",
            CreateContent(out var content),
            "assetstudio.panel.asset-inspector")
    {
        _content = content;
    }

    public event EventHandler<AssetStudioAssetParameterChangedEventArgs>?
        ParameterChanged;

    public event EventHandler<string>? ResetRequested;

    public AssetRecipe? Asset => _asset;

    public AssetInstance? Instance => _instance;

    public void SetAsset(
        AssetRecipe? asset,
        AssetInstance? instance = null)
    {
        _asset = asset;
        _instance = asset is null
            ? null
            : instance ?? new AssetInstance(asset.AssetId + ".instance", asset);
        _content.Children.Clear();
        if (_asset is null)
        {
            _content.Children.Add(
                CreateMutedText(
                    "Select an asset template to edit its exposed shape controls."));
            return;
        }

        if (_asset.Parameters.Length == 0)
        {
            _content.Children.Add(
                CreateMutedText(
                    "This asset has no exposed controls yet."));
            return;
        }

        foreach (var group in _asset.Parameters
                     .GroupBy(
                         parameter => parameter.Editor.Group ?? "Asset",
                         StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var title = new TextBlock
            {
                Text = group.Key,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 8, 0, 4),
            };
            ProfileBinding.Bind(
                title,
                TextBlock.ForegroundProperty,
                Profile.AccentResourceKey);
            _content.Children.Add(title);
            foreach (var parameter in group)
            {
                _content.Children.Add(CreateEditor(parameter));
            }
        }
    }

    private Control CreateEditor(AssetParameterDefinition parameter)
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
            $"assetstudio.asset.parameter.{parameter.Id}");

        void Commit()
        {
            if (TryParseValue(parameter.DefaultValue, value.Text, out var parsed))
            {
                ParameterChanged?.Invoke(
                    this,
                    new AssetStudioAssetParameterChangedEventArgs(
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
            Children = { label, value, reset },
        };
        Grid.SetColumnSpan(label, 2);
        Grid.SetRow(value, 1);
        Grid.SetRow(reset, 1);
        Grid.SetColumn(reset, 1);
        return grid;
    }

    private static Control CreateContent(out StackPanel content)
    {
        content = new StackPanel { Spacing = 2 };
        content.SetValue(
            AutomationProperties.NameProperty,
            "Asset exposed parameter inspector");
        content.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.asset.inspector");
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
            ScalarValue scalar => scalar.Value.ToString(
                "0.###",
                System.Globalization.CultureInfo.InvariantCulture),
            IntegerValue integer => integer.Value.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue boolean => boolean.Value ? "true" : "false",
            TextValue text => text.Value,
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
        if (template is ScalarValue scalar &&
            double.TryParse(
                text,
                System.Globalization.NumberStyles.Float,
                culture,
                out var number))
        {
            value = new ScalarValue(number, scalar.Unit);
            return true;
        }

        if (template is IntegerValue integer &&
            long.TryParse(
                text,
                System.Globalization.NumberStyles.Integer,
                culture,
                out var whole))
        {
            value = new IntegerValue(whole, integer.Unit);
            return true;
        }

        if (template is TextValue)
        {
            value = new TextValue(text);
            return true;
        }

        if (template is BooleanValue &&
            bool.TryParse(text, out var boolean))
        {
            value = new BooleanValue(boolean);
            return true;
        }

        return false;
    }
}
