using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Session;
using Novolis.Avalonia._3D.Ui;
using Novolis.Modeling.Scene;

namespace SceneLab;

internal static class Program
{
    internal static IHost ApplicationHost { get; private set; } = null!;
    internal static AgentSurface? SceneSurface { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton(_ =>
                {
                    var sample = args.Any(a => a.Equals("--cloner", StringComparison.OrdinalIgnoreCase))
                        ? SceneDocument.CreateClonerRow()
                        : args.Any(a => a.Equals("--boole", StringComparison.OrdinalIgnoreCase))
                            ? SceneDocument.CreateBooleCut()
                            : args.Any(a => a.Equals("--look", StringComparison.OrdinalIgnoreCase))
                                ? SceneDocument.CreateLookSetup()
                                : args.Any(a => a.Equals("--edit", StringComparison.OrdinalIgnoreCase))
                                    ? SceneDocument.CreateEditBox()
                                    : args.Any(a => a.Equals("--gallery", StringComparison.OrdinalIgnoreCase))
                                        ? SceneDocument.CreatePrimitiveGallery()
                                        : SceneDocument.CreatePrimitiveStage("SceneLab");
                    return new SceneSessionService(sample) { AppId = "scenelab" };
                });
                services.AddTransient<MainWindow>();
            })
            .Build();

        ApplicationHost.Start();
        try
        {
            var session = ApplicationHost.Services.GetRequiredService<SceneSessionService>();
            SceneSurface = AgentSurface.AttachAll(session, session.Definition)
                           ?? AgentSurface.TryAttachFromEnvironment(session, session.Definition);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            if (SceneSurface is not null)
                SceneSurface.DisposeAsync().AsTask().GetAwaiter().GetResult();
            ApplicationHost.StopAsync().GetAwaiter().GetResult();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

internal sealed class MainWindow : Window
{
    public MainWindow(SceneSessionService session)
    {
        Title = "Novolis SceneLab — mesh modeller";
        Width = 1600;
        Height = 920;
        MinWidth = 960;
        MinHeight = 560;
        Background = new SolidColorBrush(Color.FromRgb(14, 20, 28));

        var surface = new SceneEditorSurface(session);
        var status = new TextBlock
        {
            Margin = new Thickness(10, 4),
            FontSize = 12,
            Opacity = 0.85,
            Foreground = Brushes.WhiteSmoke,
            Text = Program.SceneSurface?.HttpBaseUrl is { } url
                ? $"Session HTTP {url}  TCP :{Program.SceneSurface.TcpPort}"
                : "Session not attached (set NOVOLIS_SCENE_SESSION=1 or AttachAll).",
            [DockPanel.DockProperty] = Dock.Bottom,
        };

        Content = new DockPanel
        {
            Children = { status, surface },
        };
    }
}
