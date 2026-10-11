using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
    private CancellationTokenSource? _requestCancellation;
    private long _requestVersion;
    private AssetStudioPreviewResult? _lastResult;

    public AssetStudioPreviewPanel(IAssetStudioPreviewService? previewService = null)
        : base(
            "Preview",
            CreateContent(out var status),
            "assetstudio.panel.preview")
    {
        _status = status;
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

            _lastResult = result;
            _status.Text = result.DisplayText;
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
    }

    private static Control CreateContent(out TextBlock status)
    {
        status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120,
            VerticalAlignment = VerticalAlignment.Center,
        };
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
            Child = status,
        };
    }
}
