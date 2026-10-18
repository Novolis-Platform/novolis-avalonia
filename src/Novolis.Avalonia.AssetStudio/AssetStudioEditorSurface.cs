using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Text.Json;
using Novolis.AssetStudio.Authoring;
using Novolis.Avalonia.GraphicalProfile;
using Novolis.Avalonia.Layout;
using Novolis.Avalonia.Studio;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;
using ProfileBinding = Novolis.Avalonia.GraphicalProfile.GraphicalProfileBinding;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;
using Novolis.Visual.Assets;
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
    private readonly AssetStudioNodeInspectorPanel _nodeInspector;
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
    private readonly AssetStudioCatalog _catalog;
    private AssetStudioAuthoringSession? _authoringSession;
    private VisualIR? _activeMaterialCompilation;

    public AssetStudioEditorSurface(
        VisualRecipe? recipe = null,
        VisualNodeRegistry? registry = null,
        IAssetStudioPreviewService? previewService = null,
        AssetStudioCatalog? catalog = null,
        VisualPresetCatalog? presets = null)
    {
        _registry = registry ?? VisualCompiler.CreateDefaultRegistry();
        _presets = presets ?? new VisualPresetCatalog([]);
        _materials = catalog?.Materials ?? new MaterialCatalog(
            _presets.Templates.SelectMany(template => template.Materials));
        _catalog = catalog ?? new AssetStudioCatalog(
            VisualNodeCatalog.FromRegistry(_registry),
            _materials,
            _presets.Templates.Select(template => template.Asset));
        _browser = new AssetStudioMaterialBrowserPanel();
        _templates = new AssetStudioAssetTemplatePanel();
        _graph = new AssetStudioGraphPanel();
        _stack = new AssetStudioStackPanel();
        _materialInspector = new AssetStudioMaterialInspectorPanel();
        _assetInspector = new AssetStudioAssetInspectorPanel();
        _nodeInspector = new AssetStudioNodeInspectorPanel();
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
            _catalog.Nodes,
            _catalog.Materials,
            _presets,
            _catalog.Assets);
        _templates.SetCatalog(_presets);
        _browser.EntrySelected += OnBrowserEntrySelected;
        _graph.NodeActionRequested += OnGraphNodeActionRequested;
        _graph.ConnectionRequested += OnGraphConnectionRequested;
        _templates.TemplateSelected += (_, template) =>
            SetAssetTemplate(template);
        _materialInspector.ParameterChanged += OnMaterialParameterChanged;
        _materialInspector.ResetRequested += OnMaterialResetRequested;
        _assetInspector.ParameterChanged += OnAssetParameterChanged;
        _assetInspector.ResetRequested += OnAssetResetRequested;
        _assetInspector.MaterialSlotChanged += OnMaterialSlotChanged;
        _nodeInspector.ValueChanged += OnNodeValueChanged;
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
                    _nodeInspector,
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

    /// <summary>Descriptor-driven graph node value editor.</summary>
    public AssetStudioNodeInspectorPanel NodeInspector => _nodeInspector;

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

    /// <summary>
    /// The authoritative immutable authoring session projected by every
    /// workspace surface.
    /// </summary>
    public AssetStudioAuthoringSession? AuthoringSession =>
        _authoringSession;

    /// <summary>The current document including material or asset context.</summary>
    public AssetStudioDocument? CurrentDocument =>
        _authoringSession?.Document;

    /// <summary>Raised when the shared command bar submits text.</summary>
    public event EventHandler<StudioCommandSubmittedEventArgs>? CommandSubmitted;

    /// <summary>Raised when source text is edited.</summary>
    public event EventHandler? SourceChanged;

    /// <summary>Loads a semantic document and refreshes every panel projection.</summary>
    public void SetDocument(AssetStudioSemanticState document)
    {
        ArgumentNullException.ThrowIfNull(document);
        LoadDocument(AssetStudioDocument.Create(document.Recipe));
    }

    /// <summary>Loads a complete material, asset, or plain recipe document.</summary>
    public void SetDocument(AssetStudioDocument document) =>
        LoadDocument(document ?? throw new ArgumentNullException(nameof(document)));

    private void LoadDocument(AssetStudioDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _authoringSession = AssetStudioAuthoringSession.Create(
            document,
            _catalog);
        ProjectDocument(document);
    }

    private void ProjectDocument(AssetStudioDocument document)
    {
        Document = document.Semantic;
        _graph.SetRecipe(document.Recipe, _catalog.Registry);
        _nodeInspector.SetNode(
            document.Recipe,
            _graph.SelectedNodeId,
            _catalog.Registry);
        _stack.SetItems(
            document.Recipe.Nodes
                .OrderBy(node => node.Id)
                .Select((node, index) => FormatStackItem(node, index + 1)));
        _source.SetSource(FormatSource(document.Recipe));
        _diagnostics.SetDiagnostics(
            AssetStudioAuthoringValidator.Validate(
                document,
                _catalog.Registry).Items);
        _assetInspector.SetAsset(
            document.Asset,
            document.AssetInstance,
            _catalog.Materials.Materials);
        _materialInspector.SetMaterial(
            document.Material,
            document.MaterialInstance);
        if (document.Material is { } material)
        {
            _instance.SetInstance(
                material,
                document.MaterialInstance);
        }
        else if (document.Asset is { } asset)
        {
            _instance.SetAssetInstance(
                asset,
                document.AssetInstance);
        }
        else
        {
            _instance.SetInstance(null, null);
        }
        _feedback.SetStatus(
            $"{document.Recipe.RecipeId} · revision {document.Revision} · " +
            $"{document.Recipe.Nodes.Length} node(s)");
        _activeMaterialCompilation = CompileDocument(document);
        if (_activeMaterialCompilation is { } compilation)
        {
            SetMaterialPreviewSurface();
            SetCompilation(compilation);
        }
        else
        {
            _ = RefreshPreviewAsync();
        }
    }

    /// <summary>
    /// Loads a no-code template and selects its first reusable material.
    /// </summary>
    public void SetAssetTemplate(VisualPresetTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        SetAsset(template.Asset);
        _feedback.SetStatus(
            $"{template.DisplayName} template loaded · no code required.");
    }

    /// <summary>Loads a reusable material graph into the material surfaces.</summary>
    public void SetMaterial(MaterialGraph material)
    {
        ArgumentNullException.ThrowIfNull(material);
        LoadDocument(AssetStudioDocument.CreateMaterial(material));
        _primaryTabs.SelectedIndex = 0;
        _contextTabs.SelectedIndex = 0;
    }

    /// <summary>Loads an assembled asset into the same authoritative session.</summary>
    public void SetAsset(AssetRecipe asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        LoadDocument(AssetStudioDocument.CreateAsset(asset));
        _primaryTabs.SelectedIndex = 0;
        _contextTabs.SelectedIndex = 0;
    }

    /// <summary>Creates a new semantic revision from a recipe.</summary>
    public void SetRecipe(VisualRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (_authoringSession is { } session)
        {
            LoadDocument(session.Document.WithRecipe(recipe));
        }
        else
        {
            SetDocument(new AssetStudioSemanticState(recipe));
        }
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
            $"fingerprint {compilation.Fingerprint[..global::System.Math.Min(
                12,
                compilation.Fingerprint.Length)]}");
        _ = RefreshPreviewAsync();
    }

    /// <summary>Attempts to parse and load the canonical source editor text.</summary>
    public bool TryApplySource(out string message)
    {
        try
        {
            var recipe = VisualRecipeSerializer.Deserialize(
                _source.Source,
                _catalog.Registry);
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

    /// <summary>Undoes the most recent accepted authoring command.</summary>
    public bool TryUndo()
    {
        if (_authoringSession is not { } session)
        {
            return false;
        }

        return ApplyAuthoringResult(session.Undo());
    }

    /// <summary>Redoes the most recently undone authoring command.</summary>
    public bool TryRedo()
    {
        if (_authoringSession is not { } session)
        {
            return false;
        }

        return ApplyAuthoringResult(session.Redo());
    }

    /// <summary>Creates and applies an instance in the authoritative session.</summary>
    public bool TryCreateInstance()
    {
        return _authoringSession is { } session &&
               ApplyAuthoringResult(session.CreateInstance());
    }

    private async Task RefreshPreviewCoreAsync(
        CancellationToken cancellationToken)
    {
        if (_authoringSession is null || Document is null)
        {
            return;
        }

        await _preview.RefreshAsync(
            _authoringSession.Document,
            cancellationToken);
        if (_activeMaterialCompilation is { Success: true } compilation &&
            _authoringSession.Document is { } document)
        {
            if (document.Asset is { } asset)
            {
                _preview.SetAssetPreview(
                    compilation,
                    asset,
                    _authoringSession.Document.AssetInstance);
            }
            else if (document.Material is { } material)
            {
                _preview.SetMaterialPreview(
                    compilation,
                    material.DisplayName);
            }
        }
    }

    private VisualIR? CompileDocument(AssetStudioDocument document)
    {
        var options = new VisualCompileOptions(
            registry: _catalog.Registry,
            strict: true);
        return document.DocumentKind switch
        {
            AssetStudioDocumentKind.Material when document.Material is { } material =>
                VisualMaterialCompiler.CompileMaterial(
                    material,
                    document.MaterialInstance,
                    options),
            AssetStudioDocumentKind.Asset when document.Asset is { } asset =>
                VisualMaterialCompiler.CompileAsset(
                    asset,
                    document.AssetInstance,
                    options),
            AssetStudioDocumentKind.Recipe =>
                VisualCompiler.Compile(document.Recipe, options),
            _ => null,
        };
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
                 _catalog.FindAsset(entry.Id) is { } asset)
        {
            SetAsset(asset);
        }
        else if (string.Equals(entry.Kind, "Material", StringComparison.Ordinal) &&
                 _materials.Search(entry.Id).FirstOrDefault() is { } material)
        {
            SetMaterial(material);
        }
        else if (string.Equals(entry.Kind, "Node", StringComparison.Ordinal) &&
                 _authoringSession is { } session)
        {
            ApplyAuthoringResult(session.InsertNode(entry.Id));
        }
    }

    private void OnGraphNodeActionRequested(
        object? sender,
        AssetStudioGraphNodeEventArgs args)
    {
        if (_authoringSession is not { } session)
        {
            return;
        }

        var result = args.Action switch
        {
            "delete" => session.RemoveNode(args.NodeId),
            "duplicate" => session.DuplicateNode(args.NodeId),
            _ => null,
        };
        if (args.Action is "select" or "inspect" or "inspect-port")
        {
            _nodeInspector.SetNode(
                session.Document.Recipe,
                args.NodeId,
                _catalog.Registry);
        }
        if (result is not null)
        {
            ApplyAuthoringResult(result);
        }
    }

    private void OnGraphConnectionRequested(
        object? sender,
        AssetStudioGraphConnectionEventArgs args)
    {
        if (_authoringSession is not { } session)
        {
            return;
        }

        ApplyAuthoringResult(
            session.Connect(
                args.Source.NodeId,
                args.Source.PortId,
                args.Target.NodeId,
                args.Target.PortId));
    }

    private void OnNodeValueChanged(
        object? sender,
        AssetStudioNodeValueChangedEventArgs args)
    {
        if (_authoringSession is not { } session)
        {
            return;
        }

        ApplyAuthoringResult(
            session.SetNodeValue(
                args.NodeId,
                args.ValueName,
                args.Value));
    }

    private void OnMaterialParameterChanged(
        object? sender,
        AssetStudioMaterialParameterChangedEventArgs args)
    {
        if (_authoringSession is null ||
            _authoringSession.Document.Material is null)
        {
            return;
        }

        var result = _authoringSession.Document.MaterialInstance is not null
            ? _authoringSession.SetMaterialInstanceOverride(
                args.ParameterId,
                args.Value)
            : _authoringSession.SetMaterialControl(
                args.ParameterId,
                args.Value);
        if (!ApplyAuthoringResult(result))
        {
            return;
        }

        _feedback.SetStatus($"Material control updated · {args.ParameterId}");
    }

    private void OnMaterialResetRequested(object? sender, string parameterId)
    {
        if (_authoringSession?.Document.Material?.FindParameter(parameterId)
            is not { } parameter)
        {
            return;
        }

        var result = _authoringSession.Document.MaterialInstance is not null
            ? _authoringSession.ResetMaterialInstanceOverride(parameterId)
            : _authoringSession.SetMaterialControl(
                parameterId,
                parameter.DefaultValue);
        if (ApplyAuthoringResult(result))
        {
            _feedback.SetStatus($"Material control reset · {parameterId}");
        }
    }

    private void OnAssetParameterChanged(
        object? sender,
        AssetStudioAssetParameterChangedEventArgs args)
    {
        if (_authoringSession?.Document.Asset is null)
        {
            return;
        }

        var result = _authoringSession.Document.AssetInstance is not null
            ? _authoringSession.SetAssetInstanceOverride(
                args.ParameterId,
                args.Value)
            : _authoringSession.SetAssetControl(
                args.ParameterId,
                args.Value);
        if (ApplyAuthoringResult(result))
        {
            _feedback.SetStatus(
                $"Asset control updated · {args.ParameterId}");
        }
    }

    private void OnMaterialSlotChanged(
        object? sender,
        AssetStudioMaterialSlotChangedEventArgs args)
    {
        if (_authoringSession is not { } session)
        {
            return;
        }

        ApplyAuthoringResult(
            session.AssignMaterial(args.SlotId, args.MaterialId));
    }

    private void OnAssetResetRequested(object? sender, string parameterId)
    {
        if (_authoringSession?.Document.Asset?.FindParameter(parameterId)
            is not { } parameter)
        {
            return;
        }

        var result = _authoringSession.Document.AssetInstance is not null
            ? _authoringSession.ResetAssetInstanceOverride(parameterId)
            : _authoringSession.SetAssetControl(
                parameterId,
                parameter.DefaultValue);
        if (ApplyAuthoringResult(result))
        {
            _feedback.SetStatus($"Asset control reset · {parameterId}");
        }
    }

    private void RefreshMaterialPreview()
    {
        if (_authoringSession is null)
        {
            _materialPreview.SetMaterial(null);
            return;
        }

        _activeMaterialCompilation = CompileDocument(
            _authoringSession.Document);
        SetMaterialPreviewSurface();
    }

    private void SetMaterialPreviewSurface()
    {
        if (_authoringSession?.Document is not { } document)
        {
            _materialPreview.SetMaterial(null);
            return;
        }

        if (document.Asset is { } asset)
        {
            _materialPreview.SetAsset(
                asset,
                document.AssetInstance,
                document.Material,
                _activeMaterialCompilation);
        }
        else if (document.Material is { } material)
        {
            _materialPreview.SetMaterial(
                material,
                _activeMaterialCompilation);
        }
        else
        {
            _materialPreview.SetMaterial(null);
        }
    }

    private bool ApplyAuthoringResult(AssetStudioAuthoringResult result)
    {
        if (!result.Succeeded)
        {
            var message = result.Diagnostics.Items.FirstOrDefault()?.Message
                ?? "The authoring edit was rejected.";
            _feedback.FlashError(message);
            _diagnostics.SetDiagnostics(result.Diagnostics.Items);
            return false;
        }

        _authoringSession = result.Session;
        ProjectDocument(_authoringSession.Document);
        return true;
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
