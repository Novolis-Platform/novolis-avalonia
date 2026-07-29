using Avalonia.Controls;
using Avalonia.Layout;
using Novolis.Avalonia.Cad.Commands;
using Novolis.Avalonia.Cad.Core;
using Novolis.Avalonia.Cad.Services;
using Novolis.Avalonia.Raylib;
using Novolis.Cad.Primitives;

namespace Novolis.Avalonia.Cad.Ui;

/// <summary>Draft/model dual viewport host (product chrome stays in the app).</summary>
public sealed class CadEditorSurface : Panel
{
    private readonly CadEditorSettings _settings;

    public CadEditorSurface(
        CadDocumentSession session,
        CadEditorSettings settings,
        CadCommandBus bus,
        CadCommandDispatcher dispatcher,
        CadToolController tools,
        CadModelRenderer modelRenderer)
    {
        Session = session;
        _settings = settings;
        CommandDispatcher = dispatcher;
        Tools = tools;
        ModelRenderer = modelRenderer;
        Bus = bus;

        DraftViewport = new CadDraftViewport(session, settings, dispatcher, bus, tools);
        ModelHost = new RaylibHostControl();
        modelRenderer.Bind(ModelHost);

        var viewportHost = new Panel();
        viewportHost.Children.Add(DraftViewport);
        viewportHost.Children.Add(ModelHost);
        Children.Add(viewportHost);

        ApplyViewMode(ParseViewMode(settings.Settings.ViewMode));
    }

    public CadDocumentSession Session { get; }

    public CadCommandBus Bus { get; }

    public CadCommandDispatcher CommandDispatcher { get; }

    public CadToolController Tools { get; }

    public CadModelRenderer ModelRenderer { get; }

    public CadDraftViewport DraftViewport { get; }

    public RaylibHostControl ModelHost { get; }

    public CadViewMode ViewMode { get; private set; } = CadViewMode.Draft;

    public event Action? ViewModeChanged;

    public void SetViewMode(CadViewMode mode)
    {
        if (ViewMode == mode)
            return;
        ApplyViewMode(mode);
        ViewModeChanged?.Invoke();
    }

    public void Fit()
    {
        if (ViewMode == CadViewMode.Model)
            ModelRenderer.Fit();
        else
            DraftViewport.Fit();
    }

    private void ApplyViewMode(CadViewMode mode)
    {
        ViewMode = mode;
        _settings.Settings.ViewMode = mode == CadViewMode.Model ? "model" : "draft";
        DraftViewport.IsVisible = mode == CadViewMode.Draft;
        ModelHost.IsVisible = mode == CadViewMode.Model;
        if (mode == CadViewMode.Model)
        {
            ModelHost.SetHostActive(true);
            ModelHost.EnsureHostStarted();
        }
        else
        {
            ModelHost.SetHostActive(false);
        }
    }

    private static CadViewMode ParseViewMode(string? raw) =>
        string.Equals(raw, "model", StringComparison.OrdinalIgnoreCase)
            ? CadViewMode.Model
            : CadViewMode.Draft;
}
