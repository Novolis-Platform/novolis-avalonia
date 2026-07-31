using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Novolis.Agent.Core;
using Novolis.Agent.Surface;
using Novolis.Avalonia._3D.Services;
using Novolis.Avalonia._3D.Session;
using Novolis.Modeling.Scene;

namespace Novolis.Avalonia._3D.Ui;

/// <summary>Opens the shaded Render popup and saves PNGs from it.</summary>
public static class SceneRenderActions
{
    private static SceneRenderWindow? _open;

    public static SceneRenderWindow? OpenWindow => _open is { IsVisible: true } ? _open : null;

    public static void ShowRenderWindow(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        var owner = TopLevel.GetTopLevel(host) as Window;
        if (_open is { IsVisible: true })
        {
            _open.Activate();
            _open.SyncFromMainViewport();
            return;
        }

        SceneViewportCamera? mainCam = host is SceneEditorSurface surface ? surface.Viewport.Camera : null;
        var win = new SceneRenderWindow(session, mainCam, notice);
        _open = win;
        win.Closed += (_, _) =>
        {
            if (ReferenceEquals(_open, win))
                _open = null;
        };

        if (owner is not null)
        {
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.Show(owner);
        }
        else
        {
            win.Show();
        }

        notice?.Invoke("Render window open — preview matched to main viewport.");
    }

    /// <summary>Push main-viewport or active-camera framing into the open render preview (if any).</summary>
    public static void SyncOpenPreviewFromMain() => OpenWindow?.SyncFromMainViewport();

    public static void SyncOpenPreviewFromActiveCamera() => OpenWindow?.SyncFromActiveCamera();

    public static void SaveRenderPng(Control host, SceneSessionService session, Action<string>? notice = null) =>
        _ = RunSafe(() => SaveRenderPngAsync(host, session, notice), notice);

    public static async Task SaveRenderPngAsync(Control host, SceneSessionService session, Action<string>? notice = null)
    {
        if (_open is null || !_open.IsVisible)
            ShowRenderWindow(host, session, notice);

        if (_open is null)
        {
            notice?.Invoke("Render window unavailable.");
            return;
        }

        await _open.SavePngAsync().ConfigureAwait(true);
    }

    public static void EnsureStudioLights(SceneSessionService session, Action<string>? notice = null)
    {
        var lights = session.Document.Nodes.OfType<LightNode>().ToList();
        if (lights.Count >= 3)
        {
            notice?.Invoke($"Scene already has {lights.Count} lights.");
            return;
        }

        void Add(string name, string kind, float intensity, float x, float y, float z, float rx, float ry, float rz)
        {
            session.Execute(new AgentCommand
            {
                ActionId = SceneSessionActionIds.AddLight,
                LightKind = kind,
                Intensity = intensity,
                Name = name,
            });
            if (session.Document.SelectionId is { } id)
            {
                session.Execute(new AgentCommand
                {
                    ActionId = SceneSessionActionIds.SetTransform,
                    NodeId = id.ToString(),
                    X = x, Y = y, Z = z,
                    Rx = rx, Ry = ry, Rz = rz,
                });
            }
        }

        if (lights.All(l => !l.Name.Contains("Key", StringComparison.OrdinalIgnoreCase)))
            Add("Key", "spot", 3.8f, 22f, 16f, 18f, 40f, -35f, 0f);
        if (lights.All(l => !l.Name.Contains("Fill", StringComparison.OrdinalIgnoreCase)))
            Add("Fill", "omni", 2.0f, 0f, 2f, -18f, 0f, 0f, 0f);
        if (lights.All(l => !l.Name.Contains("Rim", StringComparison.OrdinalIgnoreCase)))
            Add("Rim", "infinite", 0.55f, 0f, 0f, 0f, -55f, 30f, 0f);

        notice?.Invoke("Studio lights ensured (Key / Fill / Rim).");
    }

    private static async Task RunSafe(Func<Task> work, Action<string>? notice)
    {
        try
        {
            await work().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            notice?.Invoke($"Render failed: {ex.Message}");
        }
    }
}

/// <summary>Pop-up shaded render preview with ambient / exposure / light controls and Save PNG.</summary>
public sealed class SceneRenderWindow : Window
{
    private readonly SceneSessionService _session;
    private readonly SceneViewportCamera? _mainCamera;
    private readonly Action<string>? _notice;
    private readonly SceneShadedGlControl _preview;
    private readonly StackPanel _lightPanel = new() { Spacing = 6 };
    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Opacity = 0.85,
        Foreground = Brushes.WhiteSmoke,
        Margin = new Thickness(0, 8, 0, 0),
        TextWrapping = TextWrapping.Wrap,
    };

    public SceneRenderWindow(
        SceneSessionService session,
        SceneViewportCamera? mainCamera = null,
        Action<string>? notice = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _mainCamera = mainCamera;
        _notice = notice;
        Title = "Render — shaded preview";
        Width = 1100;
        Height = 720;
        MinWidth = 720;
        MinHeight = 480;
        Background = new SolidColorBrush(Color.FromRgb(22, 28, 36));

        _preview = new SceneShadedGlControl(session)
        {
            MinWidth = 420,
            MinHeight = 360,
        };

        Content = BuildLayout();
        Opened += (_, _) =>
        {
            // Match the CAD viewport — Fit() alone frames differently and looks "broken".
            if (!SyncFromMainViewport())
            {
                _preview.Fit();
                _preview.RequestPresent();
            }

            RefreshLights();
        };
        _session.DocumentChanged += RefreshLights;
        _session.LookThroughRequested += () => SyncFromActiveCamera();
    }

    public SceneViewportCamera PreviewCamera => _preview.Camera;

    /// <summary>Copy main CAD orbit into this preview. Returns false if main camera unavailable.</summary>
    public bool SyncFromMainViewport()
    {
        if (_mainCamera is null)
            return false;

        var eye = _mainCamera.Orbit.BuildEyePosition();
        var target = _mainCamera.Orbit.Target;
        _preview.Camera.ApplyFromEyeAndTarget(eye, target, _mainCamera.Orbit.FieldOfViewDegrees);
        _preview.RequestPresent();
        _status.Text = "Preview matched main viewport.";
        return true;
    }

    /// <summary>Look through the document active camera in this preview.</summary>
    public bool SyncFromActiveCamera()
    {
        if (_session.Document.ActiveCameraId is not { } id)
        {
            _status.Text = "No active camera — use Look Through on a Camera node.";
            return false;
        }

        var ev = _session.Evaluator.Cache.Cameras.FirstOrDefault(c => c.Source.Id == id);
        if (ev?.Source is not CameraNode node)
            return false;

        var target = new Vector3(node.Target[0], node.Target[1], node.Target[2]);
        _preview.Camera.ApplyFromEyeAndTarget(ev.WorldPosition, target, node.FovDeg);
        _preview.RequestPresent();
        _status.Text = $"Preview looking through '{node.Name}'.";
        return true;
    }

    public async Task SavePngAsync()
    {
        var sp = StorageProvider;
        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save render PNG",
            SuggestedFileName = $"render-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG image") { Patterns = ["*.png"] },
            ],
            DefaultExtension = "png",
        }).ConfigureAwait(true);

        if (file is null)
            return;

        var path = file.TryGetLocalPath() ?? file.Path.LocalPath;
        _preview.RequestPresent();
        await Task.Delay(50).ConfigureAwait(true);
        var ok = await _preview.CapturePngAsync(path).ConfigureAwait(true);
        var msg = ok ? $"Saved render → {path}" : "Render PNG save failed.";
        _status.Text = msg;
        _notice?.Invoke(msg);
    }

    private Control BuildLayout()
    {
        var settings = _session.RenderSettings;
        var settingsPanel = new StackPanel
        {
            Width = 300,
            Margin = new Thickness(12),
            Spacing = 10,
            Children =
            {
                Header("Render settings"),
                SliderRow("Ambient", settings.AmbientStrength, 0, 3, v => settings.AmbientStrength = (float)v),
                ColorRow("Ambient tint", settings.AmbientColor, c => settings.AmbientColor = c),
                SliderRow("Exposure", settings.Exposure, 0.2, 4, v => settings.Exposure = (float)v),
                SliderRow("Light scale", settings.LightScale, 0, 4, v => settings.LightScale = (float)v),
                ColorRow("Clear color", settings.ClearColor, c => settings.ClearColor = c),
                ColorRow("Base albedo", settings.BaseColor, c => settings.BaseColor = c),
                CheckRow("Wire overlay", settings.WireOverlay, v => settings.WireOverlay = v),
                CheckRow("Two-sided", settings.TwoSided, v => settings.TwoSided = v),
                Header("Scene lights"),
                new ScrollViewer
                {
                    MaxHeight = 220,
                    Content = _lightPanel,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        Chrome.Btn("Studio lights", () =>
                        {
                            SceneRenderActions.EnsureStudioLights(_session, m => _status.Text = m);
                            RefreshLights();
                        }),
                        Chrome.Btn("Reset", () =>
                        {
                            settings.ResetDefaults();
                            RebuildSettingsNotice();
                        }),
                    },
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Margin = new Thickness(0, 8, 0, 0),
                    Children =
                    {
                        Chrome.PrimaryBtn("Save PNG…", () => _ = SavePngAsync()),
                        Chrome.Btn("Match viewport", () =>
                        {
                            if (!SyncFromMainViewport())
                                _status.Text = "Main viewport unavailable.";
                        }),
                        Chrome.Btn("Look through", () =>
                        {
                            if (!SyncFromActiveCamera())
                                _status.Text = "Set a Camera active first (Look Through in Properties).";
                        }),
                        Chrome.Btn("Fit", () => _preview.Fit()),
                        Chrome.Btn("Close", Close),
                    },
                },
                _status,
            },
        };

        var previewHost = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(48, 68, 84)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(12, 12, 0, 12),
            Child = _preview,
            [DockPanel.DockProperty] = Dock.Left,
        };

        return new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                new Border
                {
                    Width = 324,
                    [DockPanel.DockProperty] = Dock.Right,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(40, 56, 70)),
                    BorderThickness = new Thickness(1, 0, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(18, 24, 32)),
                    Child = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Content = settingsPanel,
                    },
                },
                previewHost,
            },
        };
    }

    private void RebuildSettingsNotice() =>
        _status.Text = "Settings reset to defaults.";

    private void RefreshLights()
    {
        _lightPanel.Children.Clear();
        var lights = _session.Document.Nodes.OfType<LightNode>().ToList();
        if (lights.Count == 0)
        {
            _lightPanel.Children.Add(Chrome.Label("No lights — use Studio lights or Look tools."));
            return;
        }

        foreach (var light in lights)
        {
            var row = new StackPanel { Spacing = 2 };
            row.Children.Add(new TextBlock
            {
                Text = $"{light.Name} ({light.LightKind})",
                FontSize = 12,
                Foreground = Brushes.WhiteSmoke,
            });

            var enabled = new CheckBox
            {
                Content = "Enabled",
                IsChecked = light.Enabled,
                Foreground = Brushes.WhiteSmoke,
                FontSize = 12,
            };
            enabled.IsCheckedChanged += (_, _) =>
            {
                light.Enabled = enabled.IsChecked == true;
                _session.Evaluator.NotifyNodeChanged(light);
                // Force preview refresh without full document churn
                _session.RenderSettings.LightScale = _session.RenderSettings.LightScale;
            };

            var intensity = new Slider
            {
                Minimum = 0,
                Maximum = 8,
                Value = light.Intensity,
                Width = 240,
            };
            var valueLabel = new TextBlock
            {
                Text = $"{light.Intensity:0.00}",
                Width = 40,
                FontSize = 11,
                Foreground = Brushes.WhiteSmoke,
                VerticalAlignment = VerticalAlignment.Center,
            };
            intensity.ValueChanged += (_, _) =>
            {
                light.Intensity = (float)intensity.Value;
                valueLabel.Text = $"{light.Intensity:0.00}";
                _session.Execute(new AgentCommand
                {
                    ActionId = SceneSessionActionIds.SetLight,
                    NodeId = light.Id.ToString(),
                    Intensity = light.Intensity,
                });
            };

            row.Children.Add(enabled);
            row.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children = { intensity, valueLabel },
            });
            _lightPanel.Children.Add(row);
        }
    }

    private static TextBlock Header(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        FontSize = 11,
        FontWeight = FontWeight.SemiBold,
        LetterSpacing = 0.5,
        Foreground = new SolidColorBrush(Color.FromRgb(160, 190, 200)),
        Margin = new Thickness(0, 4, 0, 2),
    };

    private static Control SliderRow(string label, double value, double min, double max, Action<double> onChange)
    {
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, Width = 180 };
        var read = new TextBlock
        {
            Text = $"{value:0.00}",
            Width = 42,
            FontSize = 11,
            Foreground = Brushes.WhiteSmoke,
            VerticalAlignment = VerticalAlignment.Center,
        };
        slider.ValueChanged += (_, _) =>
        {
            onChange(slider.Value);
            read.Text = $"{slider.Value:0.00}";
        };
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    Width = 78,
                    FontSize = 12,
                    Foreground = Brushes.WhiteSmoke,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                slider,
                read,
            },
        };
    }

    private static Control ColorRow(string label, Vector3 color, Action<Vector3> onChange)
    {
        Slider Make(string ch, float v, Action<float> set)
        {
            var s = new Slider { Minimum = 0, Maximum = 1, Value = v, Width = 64 };
            s.ValueChanged += (_, _) => set((float)s.Value);
            ToolTip.SetTip(s, ch);
            return s;
        }

        var current = color;
        void Push() => onChange(current);
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    Width = 78,
                    FontSize = 12,
                    Foreground = Brushes.WhiteSmoke,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                Make("R", color.X, v => { current.X = v; Push(); }),
                Make("G", color.Y, v => { current.Y = v; Push(); }),
                Make("B", color.Z, v => { current.Z = v; Push(); }),
            },
        };
    }

    private static Control CheckRow(string label, bool value, Action<bool> onChange)
    {
        var box = new CheckBox
        {
            Content = label,
            IsChecked = value,
            Foreground = Brushes.WhiteSmoke,
            FontSize = 12,
        };
        box.IsCheckedChanged += (_, _) => onChange(box.IsChecked == true);
        return box;
    }
}
