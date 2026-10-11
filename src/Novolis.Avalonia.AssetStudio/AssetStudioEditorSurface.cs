using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Text.Json;
using Novolis.AssetStudio.Core;
using Novolis.Avalonia.GraphicalProfile;
using Novolis.Avalonia.Layout;
using Novolis.Avalonia.Studio;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Reusable Asset Studio editor shell composed from shared studio and layout APIs.
/// </summary>
public sealed class AssetStudioEditorSurface : UserControl
{
    private readonly VisualNodeRegistry _registry;
    private readonly AssetStudioGraphPanel _graph;
    private readonly AssetStudioStackPanel _stack;
    private readonly AssetStudioSourcePanel _source;
    private readonly AssetStudioDiagnosticsPanel _diagnostics;
    private readonly AssetStudioPreviewPanel _preview;
    private readonly StudioCommandBar _commandBar;
    private readonly StudioChrome _chrome;
    private readonly StudioFeedback _feedback;
    private readonly AuthoringWorkspace _workspace;

    public AssetStudioEditorSurface(
        VisualRecipe? recipe = null,
        VisualNodeRegistry? registry = null,
        IAssetStudioPreviewService? previewService = null)
    {
        _registry = registry ?? VisualNodeRegistry.CreateDefault();
        _graph = new AssetStudioGraphPanel();
        _stack = new AssetStudioStackPanel();
        _source = new AssetStudioSourcePanel();
        _diagnostics = new AssetStudioDiagnosticsPanel();
        _preview = new AssetStudioPreviewPanel(
            previewService ?? new CpuAssetStudioPreviewService(_registry));
        _chrome = StudioChrome.Create();
        _feedback = _chrome.CreateFeedback();
        _commandBar = new StudioCommandBar("Recipe:");

        BindStudioChrome();
        BindCommandBar();

        var context = new Grid
        {
            RowDefinitions = new RowDefinitions("*,*,*"),
        };
        Grid.SetRow(_source, 0);
        context.Children.Add(_source);
        Grid.SetRow(_preview, 1);
        context.Children.Add(_preview);
        Grid.SetRow(_diagnostics, 2);
        context.Children.Add(_diagnostics);

        var status = new Border
        {
            Padding = new Thickness(8, 2),
            Child = new StackPanel
            {
                Children =
                {
                    _chrome.FlashLine,
                    _chrome.StatusLine,
                },
            },
        };
        ProfileBinding.Bind(
            status,
            Border.BackgroundProperty,
            Profile.SurfaceResourceKey);
        ProfileBinding.Bind(
            status,
            Border.BorderBrushProperty,
            Profile.BorderResourceKey);
        status.BorderThickness = new Thickness(0, 1, 0, 0);

        _workspace = new AuthoringWorkspace(
            nav: _stack,
            primary: _graph,
            context: context,
            topBar: _commandBar,
            statusBar: status)
        {
            NavWidth = 280,
            ContextWidth = 390,
            NarrowWidthThreshold = 940,
        };

        var root = new Border
        {
            Child = _workspace,
        };
        ProfileBinding.Bind(
            root,
            Border.BackgroundProperty,
            Profile.BackgroundResourceKey);
        SetValue(
            global::Avalonia.Automation.AutomationProperties.NameProperty,
            "Asset Studio editor");
        SetValue(
            global::Avalonia.Automation.AutomationProperties.AutomationIdProperty,
            "assetstudio.editor.surface");
        Content = root;

        _commandBar.Submitted += (_, args) =>
            CommandSubmitted?.Invoke(this, args);
        _source.SourceChanged += (_, _) =>
        {
            _feedback.SetStatus("Source edited · type validate to apply and compile.");
            SourceChanged?.Invoke(this, EventArgs.Empty);
        };

        if (recipe is not null)
        {
            SetRecipe(recipe);
        }
        else
        {
            _feedback.SetStatus("No recipe loaded.");
        }
    }

    /// <summary>Shared command bar used by the host to submit authoring actions.</summary>
    public StudioCommandBar CommandBar => _commandBar;

    /// <summary>Shared studio feedback helper.</summary>
    public StudioFeedback Feedback => _feedback;

    /// <summary>Adaptive Layout workspace hosting all editor regions.</summary>
    public AuthoringWorkspace Workspace => _workspace;

    /// <summary>Graph and accessible ordered node/port surface.</summary>
    public AssetStudioGraphPanel Graph => _graph;

    /// <summary>Ordered stack projection.</summary>
    public AssetStudioStackPanel Stack => _stack;

    /// <summary>Canonical source editor.</summary>
    public AssetStudioSourcePanel Source => _source;

    /// <summary>Validation and compile diagnostics.</summary>
    public AssetStudioDiagnosticsPanel Diagnostics => _diagnostics;

    /// <summary>Optional preview status surface.</summary>
    public AssetStudioPreviewPanel Preview => _preview;

    /// <summary>Current semantic document, when one has been loaded.</summary>
    public AssetStudioSemanticState? Document { get; private set; }

    /// <summary>Raised when the shared command bar submits text.</summary>
    public event EventHandler<StudioCommandSubmittedEventArgs>? CommandSubmitted;

    /// <summary>Raised when source text is edited.</summary>
    public event EventHandler? SourceChanged;

    /// <summary>Loads a semantic document and refreshes every panel projection.</summary>
    public void SetDocument(AssetStudioSemanticState document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
        _graph.SetRecipe(document.Recipe, _registry);
        _stack.SetItems(
            document.Recipe.Nodes
                .OrderBy(node => node.Id)
                .Select((node, index) => FormatStackItem(node, index + 1)));
        _source.SetSource(FormatSource(document.Recipe));
        _diagnostics.SetDiagnostics(document.Recipe.Validate(_registry).Diagnostics);
        _feedback.SetStatus(
            $"{document.Recipe.RecipeId} · revision {document.Revision} · " +
            $"{document.Recipe.Nodes.Length} node(s)");
        _ = RefreshPreviewAsync();
    }

    /// <summary>Creates a new semantic revision from a recipe.</summary>
    public void SetRecipe(VisualRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var revision = Document is null ? 0 : Document.Revision + 1;
        SetDocument(new AssetStudioSemanticState(recipe, revision));
    }

    /// <summary>Displays compiler output and its normalized diagnostics.</summary>
    public void SetCompilation(VisualIR compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        _diagnostics.SetDiagnostics(compilation);
        var suffix = compilation.Success ? "valid" : "needs attention";
        _feedback.SetStatus(
            $"{compilation.RecipeId} · {suffix} · " +
            $"{compilation.Nodes.Length} normalized node(s) · " +
            $"fingerprint {compilation.Fingerprint[..Math.Min(12, compilation.Fingerprint.Length)]}");
        _ = RefreshPreviewAsync();
    }

    /// <summary>Attempts to parse and load the canonical source editor text.</summary>
    public bool TryApplySource(out string message)
    {
        try
        {
            var recipe = VisualRecipeSerializer.Deserialize(
                _source.Source,
                _registry);
            SetRecipe(recipe);
            message = "Source applied.";
            return true;
        }
        catch (Exception exception)
        {
            message = $"Source rejected · {exception.Message}";
            _feedback.FlashError(message);
            return false;
        }
    }

    /// <summary>Requests an optional preview without requiring a graphics backend.</summary>
    public Task RefreshPreviewAsync(CancellationToken cancellationToken = default) =>
        Document is null
            ? Task.CompletedTask
            : _preview.RefreshAsync(Document, cancellationToken);

    private void BindStudioChrome()
    {
        ProfileBinding.Bind(
            _chrome.StatusLine,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey);
        ProfileBinding.Bind(
            _chrome.FlashLine,
            TextBlock.ForegroundProperty,
            Profile.AccentResourceKey);
        ProfileBinding.Bind(
            _chrome.BusyText,
            TextBlock.ForegroundProperty,
            Profile.TextResourceKey);
        ProfileBinding.Bind(
            _chrome.BusyOverlay,
            Border.BackgroundProperty,
            Profile.RaisedResourceKey);
        _chrome.BusyOverlay.Opacity = 0.96;
    }

    private void BindCommandBar()
    {
        if (_commandBar.Content is not Border border)
        {
            return;
        }

        ProfileBinding.Bind(
            border,
            Border.BackgroundProperty,
            Profile.SurfaceResourceKey);
        ProfileBinding.Bind(
            border,
            Border.BorderBrushProperty,
            Profile.BorderResourceKey);

        if (border.Child is not Panel row)
        {
            return;
        }

        foreach (var textBlock in row.Children.OfType<TextBlock>())
        {
            ProfileBinding.Bind(
                textBlock,
                TextBlock.ForegroundProperty,
                Profile.TextResourceKey);
        }

        foreach (var textBox in row.Children.OfType<TextBox>())
        {
            ProfileBinding.Bind(
                textBox,
                TextBox.BackgroundProperty,
                Profile.RaisedResourceKey);
            ProfileBinding.Bind(
                textBox,
                TextBox.ForegroundProperty,
                Profile.TextResourceKey);
            ProfileBinding.Bind(
                textBox,
                TextBox.BorderBrushProperty,
                Profile.BorderResourceKey);
        }
    }

    private string FormatStackItem(VisualNode node, int ordinal)
    {
        var label = _registry.TryGetDescriptor(node.TypeId, out var descriptor)
            ? descriptor?.DisplayName ?? node.TypeId
            : $"{node.TypeId} (unknown)";
        var values = node.Values.Count == 0
            ? string.Empty
            : $" · {string.Join(", ", node.Values.Select(
                value => $"{value.Key}={FormatValue(value.Value)}"))}";
        return $"N{ordinal:00} · {label}{values}";
    }

    private static string FormatSource(VisualRecipe recipe)
    {
        using var document = JsonDocument.Parse(
            VisualRecipeSerializer.Serialize(recipe));
        return JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });
    }

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
}
