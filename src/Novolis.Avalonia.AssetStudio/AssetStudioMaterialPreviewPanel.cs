using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Novolis.Visual.Assets;
using Novolis.Visual.Compile;
using Novolis.Visual.Materials;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Material preview viewport status and preview-shape selector.
/// </summary>
public sealed class AssetStudioMaterialPreviewPanel : AssetStudioPanel
{
    private readonly TextBlock _summary;
    private readonly Image _image;
    private readonly ComboBox _shape;
    private readonly ComboBox _target;
    private AssetRecipe? _asset;
    private AssetInstance? _assetInstance;

    public AssetStudioMaterialPreviewPanel()
        : base(
            "Material Preview",
            CreateContent(out var summary, out var image, out var shape, out var target),
            "assetstudio.panel.material-preview")
    {
        _summary = summary;
        _image = image;
        _shape = shape;
        _target = target;
    }

    public ComboBox ShapeSelector => _shape;

    public ComboBox TargetSelector => _target;

    /// <summary>Rendered image control shown in the primary viewport.</summary>
    public Image PreviewImage => _image;

    /// <summary>Current human-readable preview status.</summary>
    public string Summary => _summary.Text ?? string.Empty;

    public void SetMaterial(
        MaterialGraph? graph,
        VisualIR? compilation = null)
    {
        _asset = null;
        _assetInstance = null;
        SetPreview(graph, compilation);
    }

    /// <summary>
    /// Shows an assembled no-code asset in the viewport while retaining the
    /// selected material as its active surface.
    /// </summary>
    public void SetAsset(
        AssetRecipe? asset,
        AssetInstance? instance,
        MaterialGraph? graph,
        VisualIR? compilation = null)
    {
        _asset = asset;
        _assetInstance = instance;
        SetPreview(graph, compilation);
    }

    private void SetPreview(
        MaterialGraph? graph,
        VisualIR? compilation)
    {
        if (graph is null && _asset is null)
        {
            _summary.Text = "Select a material to preview it on a sphere, strip, or asset.";
            _image.Source = null;
            _image.IsVisible = false;
            return;
        }

        var channels = graph is null || graph.Channels.Length == 0
            ? "no bound channels"
            : string.Join(
                ", ",
                graph.Channels.Select(channel => channel.Channel.ToString()));
        var target = _target.SelectedItem?.ToString() ?? "CPU Reference";
        var status = compilation is null
            ? "not compiled"
            : compilation.Success
                ? "ready"
                : "needs attention";
        _summary.Text = _asset is { } asset
            ? $"{asset.DisplayName} · {target} · {status}\n" +
              $"Material slots: {asset.MaterialSlots.Length}\n" +
              "Asset viewport: generic assembly"
            : $"{graph!.DisplayName} · {target} · {status}\n" +
              $"Channels: {channels}\n" +
              "Preview shape: " +
              (_shape.SelectedItem?.ToString() ?? "Material Sphere");
        if (target is not ("CPU Reference" or "Deterministic Path Trace"))
        {
            _summary.Text +=
                "\nUnsupported · this target has no registered execution path.";
            _image.Source = null;
            _image.IsVisible = false;
            return;
        }

        if (compilation is { Success: true })
        {
            try
            {
                var png = _asset is null
                    ? AssetStudioActualPreviewRenderer.Render(
                        compilation,
                        _shape.SelectedItem?.ToString() ?? "Material Sphere",
                        width: 360,
                        height: 260)
                    : AssetStudioActualPreviewRenderer.RenderAsset(
                        compilation,
                        _asset,
                        _assetInstance,
                        width: 360,
                        height: 260);
                using var stream = new MemoryStream(
                    png,
                    writable: false);
                _image.Source = new Bitmap(stream);
                _image.IsVisible = true;
            }
            catch (Exception exception)
            {
                _image.Source = null;
                _image.IsVisible = false;
                _summary.Text += $"\nThumbnail unavailable · {exception.Message}";
            }
        }
        else
        {
            _image.Source = null;
            _image.IsVisible = false;
        }
    }

    private static Control CreateContent(
        out TextBlock summary,
        out Image image,
        out ComboBox shape,
        out ComboBox target)
    {
        shape = new ComboBox
        {
            ItemsSource = new[]
            {
                "Material Sphere",
                "Flat Strip",
                "Selected Asset",
            },
            SelectedIndex = 0,
            MinWidth = 140,
        };
        target = new ComboBox
        {
            ItemsSource = new[]
            {
                "CPU Reference",
                "Deterministic Path Trace",
                "Raster / HDR (Unsupported)",
                "Bake (Unsupported)",
            },
            SelectedIndex = 0,
            MinWidth = 140,
        };
        summary = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 100,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ProfileBinding.Bind(
            summary,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey);
        summary.SetValue(
            AutomationProperties.NameProperty,
            "Material preview summary");
        summary.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.material-preview.summary");
        image = new Image
        {
            Stretch = global::Avalonia.Media.Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsVisible = false,
        };
        image.SetValue(
            AutomationProperties.NameProperty,
            "Asset Studio material thumbnail");
        image.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.material-preview.image");
        var selectors = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Shape" },
                shape,
                new TextBlock { Text = "Target" },
                target,
            },
        };
        foreach (var text in selectors.Children.OfType<TextBlock>())
        {
            ProfileBinding.Bind(
                text,
                TextBlock.ForegroundProperty,
                Profile.MutedResourceKey);
        }

        var viewport = new Border
        {
            Height = 300,
            Padding = new Thickness(8),
            Child = image,
        };
        ProfileBinding.Bind(
            viewport,
            Border.BackgroundProperty,
            Profile.RaisedResourceKey);
        ProfileBinding.Bind(
            viewport,
            Border.BorderBrushProperty,
            Profile.BorderResourceKey);
        viewport.BorderThickness = new Thickness(1);

        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                selectors,
                viewport,
                new Border
                {
                    Padding = new Thickness(12),
                    Child = summary,
                },
            },
        };
    }
}
