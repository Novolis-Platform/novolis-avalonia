using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Novolis.AssetStudio.Core;
using Novolis.Avalonia.GraphicalProfile;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Preview status surface that keeps the editor offline-capable.
/// </summary>
public sealed class AssetStudioPreviewPanel : AssetStudioPanel
{
    private readonly TextBlock _status;
    private readonly Image _image;
    private CancellationTokenSource? _requestCancellation;
    private long _requestVersion;
    private AssetStudioPreviewResult? _lastResult;

    public AssetStudioPreviewPanel(IAssetStudioPreviewService? previewService = null)
        : base(
            "Preview",
            CreateContent(out var status, out var image),
            "assetstudio.panel.preview")
    {
        _status = status;
        _image = image;
        PreviewService = previewService;
        SetUnavailable();
    }

    /// <summary>Optional adapter used to request a live or baked preview.</summary>
    public IAssetStudioPreviewService? PreviewService { get; set; }

    /// <summary>Last successful preview status, if any.</summary>
    public AssetStudioPreviewResult? LastResult => _lastResult;

    /// <summary>Requests a preview when an adapter is available.</summary>
    public async Task RefreshAsync(
        AssetStudioSemanticState document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var service = PreviewService;
        if (service is null)
        {
            SetUnavailable();
            return;
        }

        _requestCancellation?.Cancel();
        _requestCancellation?.Dispose();
        _requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        var requestToken = _requestCancellation.Token;
        var version = ++_requestVersion;
        _status.Text = "Preview requested…";

        try
        {
            var result = await service.RequestAsync(document, requestToken);
            if (version != _requestVersion || requestToken.IsCancellationRequested)
            {
                return;
            }

            var previous = _lastResult;
            _lastResult = result;
            _status.Text = result.DisplayText;
            if (result.PngBytes is { Length: > 0 } png)
            {
                using var stream = new MemoryStream(png, writable: false);
                _image.Source = new Bitmap(stream);
                _image.IsVisible = true;
            }
            else if (previous?.PngBytes is not { Length: > 0 })
            {
                _image.Source = null;
                _image.IsVisible = false;
            }
            else
            {
                _status.Text += " · last valid frame retained";
            }
        }
        catch (OperationCanceledException) when (requestToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version != _requestVersion)
            {
                return;
            }

            _status.Text = _lastResult is null
                ? $"Preview failed · {exception.Message}"
                : $"Last valid preview retained · stale · {exception.Message}";
        }
    }

    /// <summary>Shows the explicit no-adapter state.</summary>
    public void SetUnavailable()
    {
        _status.Text = "Preview service unavailable · editor remains offline-capable.";
        _image.Source = null;
        _image.IsVisible = false;
    }

    private static Control CreateContent(
        out TextBlock status,
        out Image image)
    {
        status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
            VerticalAlignment = VerticalAlignment.Center,
        };
        image = new Image
        {
            Stretch = global::Avalonia.Media.Stretch.Uniform,
            MaxHeight = 180,
            IsVisible = false,
        };
        image.SetValue(
            AutomationProperties.NameProperty,
            "Asset Studio rendered preview");
        image.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.preview.image");
        status.SetValue(
            AutomationProperties.NameProperty,
            "Asset Studio preview status");
        status.SetValue(
            AutomationProperties.AutomationIdProperty,
            "assetstudio.preview.status");
        ProfileBinding.Bind(
            status,
            TextBlock.ForegroundProperty,
            Profile.MutedResourceKey);
        return new Border
        {
            Padding = new Thickness(8),
            Child = new StackPanel
            {
                Spacing = 8,
                Children = { image, status },
            },
        };
    }
}
