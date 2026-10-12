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
using Novolis.Visual.Materials;
using Novolis.Visual.Presets;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Reusable Asset Studio editor shell composed from shared studio and layout APIs.
/// </summary>
public sealed class AssetStudioEditorSurface : UserControl
{
    private readonly VisualNodeRegistry _registry;
    private readonly VisualPresetCatalog _presets;
    private readonly MaterialCatalog _materials;
    private readonly AssetStudioMaterialBrowserPanel _browser;
    private readonly AssetStudioAssetTemplatePanel _templates;
    private readonly AssetStudioGraphPanel _graph;
    private readonly AssetStudioStackPanel _stack;
    private readonly AssetStudioMaterialInspectorPanel _materialInspector;
    private readonly AssetStudioAssetInspectorPanel _assetInspector;
    private readonly AssetStudioMaterialPreviewPanel _materialPreview;
    private readonly AssetStudioInstancePanel _instance;
    private readonly AssetStudioCapabilityStatusPanel _capabilities;
    private readonly AssetStudioActionBar _actionBar;
    private readonly AssetStudioSourcePanel _source;
    private readonly AssetStudioDiagnosticsPanel _diagnostics;
    private readonly AssetStudioPreviewPanel _preview;
    private readonly StudioCommandBar _commandBar;
    private readonly StudioChrome _chrome;
    private readonly StudioFeedback _feedback;
    private readonly AuthoringWorkspace _workspace;
    private readonly TabControl _primaryTabs;
    private readonly TabControl _contextTabs;
    private AssetStudioMaterialSession? _materialSession;
    private AssetStudioAssetSession? _assetSession;
    private VisualIR? _activeMaterialCompilation;

    public AssetStudioEditorSurface(
        VisualRecipe? recipe = null,
        VisualNodeRegistry? registry = null,
        IAssetStudioPreviewService? previewService = null)
    {
        _registry = registry ?? VisualCompiler.CreateDefaultRegistry();
        _presets = VisualPresetCatalog.CreateDefault();
        _materials = new MaterialCatalog(
            _presets.Templates.SelectMany(template => template.Materials));
        _browser = new AssetStudioMaterialBrowserPanel();
        _templates = new AssetStudioAssetTemplatePanel();
        _graph = new AssetStudioGraphPanel();
        _stack = new AssetStudioStackPanel();
        _materialInspector = new AssetStudioMaterialInspectorPanel();
        _assetInspector = new AssetStudioAssetInspectorPanel();
        _materialPreview = new AssetStudioMaterialPreviewPanel();
        _instance = new AssetStudioInstancePanel();
        _capabilities = new AssetStudioCapabilityStatusPanel();
        _actionBar = new AssetStudioActionBar();
        _source = new AssetStudioSourcePanel();
        _diagnostics = new AssetStudioDiagnosticsPanel();
        _preview = new AssetStudioPreviewPanel(
            previewService ?? new CpuAssetStudioPreviewService(_registry));
        _chrome = StudioChrome.Create();
        _feedback = _chrome.CreateFeedback();
        _commandBar = new StudioCommandBar("Recipe:");
        _actionBar.ActionRequested += (_, args) =>
            CommandSubmitted?.Invoke(
                this,
                new StudioCommandSubmittedEventArgs(args.Command));

        _browser.SetCatalogs(
            VisualNodeCatalog.FromRegistry(_registry),
            _materials,
            _presets,
            _presets.Templates.Select(template => template.Asset));
        _browser.EntrySelected += OnBrowserEntrySelected;
        _templates.TemplateSelected += (_, template) =>
            SetAssetTemplate(template);
        _materialInspector.ParameterChanged += OnMaterialParameterChanged;
        _materialInspector.ResetRequested += OnMaterialResetRequested;
        _assetInspector.ParameterChanged += OnAssetParameterChanged;
        _assetInspector.ResetRequested += OnAssetResetRequested;
        _materialPreview.ShapeSelector.SelectionChanged += (_, _) =>
            RefreshMaterialPreview();
        _materialPreview.TargetSelector.SelectionChanged += (_, _) =>
            RefreshMaterialPreview();
        BindStudioChrome();
        BindCommandBar();

        var nav = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                _browser,
                _templates,
            },
        };

        _primaryTabs = new TabControl();
        _primaryTabs.Items.Add(
            new TabItem
            {
                Header = "Viewport",
                Content = _materialPreview,
            });
        _primaryTabs.Items.Add(
            new TabItem
            {
                Header = "Graph",
                Content = _graph,
            });
        _primaryTabs.SetValue(
            global::Avalonia.Automation.AutomationProperties.NameProperty,
            "Asset Studio primary workspace");
        _primaryTabs.SetValue(
            global::Avalonia.Automation.AutomationProperties.AutomationIdProperty,
            "assetstudio.primary.tabs");

        _contextTabs = new TabControl();
        _contextTabs.Items.Add(
            new TabItem
            {
                Header = "Controls",
                Content = CreateContextPage(
                    _materialInspector,
                    _assetInspector,
                    _instance),
            });
        _contextTabs.Items.Add(
            new TabItem
            {
                Header = "Layers",
                Content = CreateContextPage(
                    _stack,
                    _capabilities),
            });
        _contextTabs.Items.Add(
            new TabItem
            {
                Header = "Build",
                Content = CreateContextPage(
                    _preview,
                    _diagnostics,
                    _source),
            });
        _contextTabs.SetValue(
            global::Avalonia.Automation.AutomationProperties.NameProperty,
            "Asset Studio context workspace");
        _contextTabs.SetValue(
            global::Avalonia.Automation.AutomationProperties.AutomationIdProperty,
            "assetstudio.context.tabs");

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
            nav: nav,
            primary: _primaryTabs,
            context: _contextTabs,
            topBar: new StackPanel
            {
                Children = { _actionBar, _commandBar },
            },
            statusBar: status)
        {
            NavWidth = 280,
            ContextWidth = 430,
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

    /// <summary>Button-driven front door for common no-code actions.</summary>
    public AssetStudioActionBar ActionBar => _actionBar;

    /// <summary>Shared studio feedback helper.</summary>
    public StudioFeedback Feedback => _feedback;

    /// <summary>Adaptive Layout workspace hosting all editor regions.</summary>
    public AuthoringWorkspace Workspace => _workspace;

    /// <summary>Viewport-first primary tabs; graph remains available as an advanced view.</summary>
    public TabControl PrimaryTabs => _primaryTabs;

    /// <summary>Persistent right-hand controls, layers, and build tabs.</summary>
    public TabControl ContextTabs => _contextTabs;

    /// <summary>Graph and accessible ordered node/port surface.</summary>
    public AssetStudioGraphPanel Graph => _graph;

    /// <summary>Ordered stack projection.</summary>
    public AssetStudioStackPanel Stack => _stack;

    /// <summary>Searchable material, asset, template, and node browser.</summary>
    public AssetStudioMaterialBrowserPanel Browser => _browser;

    /// <summary>No-code asset starter template list.</summary>
    public AssetStudioAssetTemplatePanel Templates => _templates;

    /// <summary>Friendly exposed material parameter editor.</summary>
    public AssetStudioMaterialInspectorPanel MaterialInspector =>
        _materialInspector;

    /// <summary>Friendly exposed asset parameter editor.</summary>
    public AssetStudioAssetInspectorPanel AssetInspector => _assetInspector;

    /// <summary>Material preview-shape and target surface.</summary>
    public AssetStudioMaterialPreviewPanel MaterialPreview =>
        _materialPreview;

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

    /// <summary>
    /// Loads a no-code template and selects its first reusable material.
    /// </summary>
    public void SetAssetTemplate(VisualPresetTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _activeMaterialCompilation = null;
        SetRecipe(template.Asset.Graph);
        _assetSession = new AssetStudioAssetSession(template.Asset);
        _assetInspector.SetAsset(
            _assetSession.Asset,
            _assetSession.Instance);
        _materialSession = template.Materials.FirstOrDefault() is { } material
            ? new AssetStudioMaterialSession(material)
            : null;
        _activeMaterialCompilation = _materialSession?.Compile(
            new VisualCompileOptions(
                registry: _registry,
                strict: true));
        _materialInspector.SetMaterial(
            _materialSession?.Graph,
            _materialSession?.Instance);
        _stack.SetItems(
            _materialSession?.Graph.Layers.Select(
                layer => $"{layer.Order:00} · {layer.Name}" +
                         (layer.Muted ? " · muted" : string.Empty))
            ?? Array.Empty<string>());
        _instance.SetInstance(
            _materialSession?.Graph,
            _materialSession?.Instance);
        SetMaterialPreviewSurface();
        _primaryTabs.SelectedIndex = 0;
        _contextTabs.SelectedIndex = 0;
        SetCompilation(_assetSession.Compile(
            new VisualCompileOptions(
                registry: _registry,
                strict: true)));
        _feedback.SetStatus(
            $"{template.DisplayName} template loaded · no code required.");
    }

    /// <summary>Loads a reusable material graph into the material surfaces.</summary>
    public void SetMaterial(MaterialGraph material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _activeMaterialCompilation = null;
        _assetSession = null;
        _assetInspector.SetAsset(null);
        _materialSession = new AssetStudioMaterialSession(material);
        SetRecipe(material.Recipe);
        _materialInspector.SetMaterial(
            _materialSession.Graph,
            _materialSession.Instance);
        _stack.SetItems(
            material.Layers.Select(
                layer => $"{layer.Order:00} · {layer.Name}" +
                         (layer.Muted ? " · muted" : string.Empty)));
        _instance.SetInstance(
            _materialSession.Graph,
            _materialSession.Instance);
        var compilation = _materialSession.Compile(
            new VisualCompileOptions(
                registry: _registry,
                strict: true));
        _activeMaterialCompilation = compilation;
        _materialPreview.SetMaterial(_materialSession.Graph, compilation);
        _primaryTabs.SelectedIndex = 0;
        _contextTabs.SelectedIndex = 0;
        SetCompilation(compilation);
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
        _capabilities.SetReport(compilation.Capabilities);
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
        RefreshPreviewCoreAsync(cancellationToken);

    private async Task RefreshPreviewCoreAsync(
        CancellationToken cancellationToken)
    {
        if (Document is null)
        {
            return;
        }

        await _preview.RefreshAsync(Document, cancellationToken);
        if (_activeMaterialCompilation is { Success: true } material)
        {
            if (_assetSession is { } assetSession)
            {
                _preview.SetAssetPreview(
                    material,
                    assetSession.Asset,
                    assetSession.Instance);
            }
            else
            {
                _preview.SetMaterialPreview(
                    material,
                    _materialSession?.Graph.DisplayName ?? "Material");
            }
        }
    }

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

    private void OnBrowserEntrySelected(
        object? sender,
        AssetStudioBrowserEntry entry)
    {
        if (string.Equals(entry.Kind, "Template", StringComparison.Ordinal) &&
            _presets.Find(entry.Id) is { } template)
        {
            SetAssetTemplate(template);
        }
        else if (string.Equals(entry.Kind, "Asset", StringComparison.Ordinal) &&
                 _presets.Templates.FirstOrDefault(
                     candidate => string.Equals(
                         candidate.Asset.AssetId,
                         entry.Id,
                         StringComparison.Ordinal)) is { } assetTemplate)
        {
            SetAssetTemplate(assetTemplate);
        }
        else if (string.Equals(entry.Kind, "Material", StringComparison.Ordinal) &&
                 _materials.Search(entry.Id).FirstOrDefault() is { } material)
        {
            SetMaterial(material);
        }
    }

    private void OnMaterialParameterChanged(
        object? sender,
        AssetStudioMaterialParameterChangedEventArgs args)
    {
        if (_materialSession is null)
        {
            return;
        }

        try
        {
            _materialSession = _materialSession.SetOverride(
                args.ParameterId,
                args.Value);
            _materialInspector.SetMaterial(
                _materialSession.Graph,
                _materialSession.Instance);
            _instance.SetInstance(
                _materialSession.Graph,
                _materialSession.Instance);
            var compilation = _materialSession.Compile(
                new VisualCompileOptions(
                    registry: _registry,
                    strict: true));
            _activeMaterialCompilation = compilation;
            SetRecipe(_materialSession.ResolvedRecipe);
            _stack.SetItems(
                _materialSession.Graph.Layers.Select(
                    layer => $"{layer.Order:00} · {layer.Name}" +
                             (layer.Muted ? " · muted" : string.Empty)));
            SetMaterialPreviewSurface();
            _feedback.SetStatus(
                $"Material override updated · {args.ParameterId}");
        }
        catch (Exception exception)
        {
            _feedback.FlashError($"Material control rejected · {exception.Message}");
        }
    }

    private void OnMaterialResetRequested(object? sender, string parameterId)
    {
        if (_materialSession is null)
        {
            return;
        }

        _materialSession = _materialSession.ResetOverride(parameterId);
        _materialInspector.SetMaterial(
            _materialSession.Graph,
            _materialSession.Instance);
        _instance.SetInstance(
            _materialSession.Graph,
            _materialSession.Instance);
        var compilation = _materialSession.Compile(
            new VisualCompileOptions(
                registry: _registry,
                strict: true));
        _activeMaterialCompilation = compilation;
        SetRecipe(_materialSession.ResolvedRecipe);
        _stack.SetItems(
            _materialSession.Graph.Layers.Select(
                layer => $"{layer.Order:00} · {layer.Name}" +
                         (layer.Muted ? " · muted" : string.Empty)));
        SetMaterialPreviewSurface();
        _feedback.SetStatus($"Material override reset · {parameterId}");
    }

    private void OnAssetParameterChanged(
        object? sender,
        AssetStudioAssetParameterChangedEventArgs args)
    {
        if (_assetSession is null)
        {
            return;
        }

        try
        {
            _assetSession = _assetSession.Apply(
                new SetAssetParameterCommand(
                    args.ParameterId,
                    args.Value));
            _assetInspector.SetAsset(
                _assetSession.Asset,
                _assetSession.Instance);
            var compilation = _assetSession.Compile(
                new VisualCompileOptions(
                    registry: _registry,
                    strict: true));
            SetRecipe(_assetSession.ResolvedRecipe);
            SetCompilation(compilation);
            SetMaterialPreviewSurface();
            _feedback.SetStatus(
                $"Asset override updated · {args.ParameterId}");
        }
        catch (Exception exception)
        {
            _feedback.FlashError(
                $"Asset control rejected · {exception.Message}");
        }
    }

    private void OnAssetResetRequested(object? sender, string parameterId)
    {
        if (_assetSession is null)
        {
            return;
        }

        _assetSession = _assetSession.Apply(
            new ResetAssetParameterCommand(parameterId));
        _assetInspector.SetAsset(
            _assetSession.Asset,
            _assetSession.Instance);
        var compilation = _assetSession.Compile(
            new VisualCompileOptions(
                registry: _registry,
                strict: true));
        SetRecipe(_assetSession.ResolvedRecipe);
        SetCompilation(compilation);
        SetMaterialPreviewSurface();
        _feedback.SetStatus($"Asset override reset · {parameterId}");
    }

    private void RefreshMaterialPreview()
    {
        if (_materialSession is null)
        {
            _materialPreview.SetMaterial(null);
            return;
        }

        _activeMaterialCompilation = _materialSession.Compile(
            new VisualCompileOptions(
                registry: _registry,
                strict: true));
        SetMaterialPreviewSurface();
    }

    private void SetMaterialPreviewSurface()
    {
        if (_materialSession is null)
        {
            _materialPreview.SetMaterial(null);
            return;
        }

        if (_assetSession is { } assetSession)
        {
            _materialPreview.SetAsset(
                assetSession.Asset,
                assetSession.Instance,
                _materialSession.Graph,
                _activeMaterialCompilation);
        }
        else
        {
            _materialPreview.SetMaterial(
                _materialSession.Graph,
                _activeMaterialCompilation);
        }
    }

    private static Control CreateContextPage(params Control[] controls)
    {
        var stack = new StackPanel
        {
            Spacing = 8,
        };
        foreach (var control in controls)
        {
            stack.Children.Add(control);
        }

        return new ScrollViewer
        {
            Content = stack,
            VerticalScrollBarVisibility =
                global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
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
