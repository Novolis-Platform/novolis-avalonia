using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.Video.Edit;

namespace Novolis.Avalonia.Video;

/// <summary>Edits the outgoing transition on the selected storyboard clip.</summary>
public sealed class TransitionInspectorControl : MovieEditPane
{
    readonly TextBlock _clipLabel = new() { Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) };
    readonly ComboBox _kind = new()
    {
        ItemsSource = new[] { TransitionKind.None, TransitionKind.Fade, TransitionKind.Wipe },
        SelectedIndex = 0,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Margin = new Thickness(0, 0, 0, 8),
    };
    readonly Slider _duration = new()
    {
        Minimum = 0,
        Maximum = 2,
        TickFrequency = 0.1,
        IsSnapToTickEnabled = true,
        Value = 0.5,
        Margin = new Thickness(0, 0, 0, 4),
    };
    readonly TextBlock _durationLabel = new() { Foreground = Brushes.White, FontSize = 12 };
    readonly Button _apply = new()
    {
        Content = "Apply transition",
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Padding = new Thickness(10, 8),
        Background = MovieEditPalette.Amber,
        Foreground = Brushes.Black,
        Margin = new Thickness(0, 8, 0, 0),
    };

    MovieProject? _project;
    TimelineClip? _clip;

    /// <summary>Creates the inspector (disabled until a clip is selected).</summary>
    public TransitionInspectorControl()
    {
        Title = "Transition";
        _kind.SelectionChanged += (_, _) => SyncDurationEnabled();
        _duration.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                _durationLabel.Text = $"Duration: {_duration.Value:0.0}s (end of clip → next)";
        };
        _durationLabel.Text = "Duration: 0.5s (end of clip → next)";
        _apply.Click += (_, _) => Apply();

        Body = new StackPanel
        {
            Children =
            {
                _clipLabel,
                new TextBlock { Text = "Outgoing effect", Foreground = MovieEditPalette.Accent, Margin = new Thickness(0, 0, 0, 4) },
                _kind,
                _durationLabel,
                _duration,
                new TextBlock
                {
                    Text = "Select a storyboard clip, choose Fade or Wipe, set duration, then Apply.",
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 180, 190)),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 0),
                },
                _apply,
            },
        };
        IsEnabled = false;
    }

    /// <summary>Raised after a transition is applied.</summary>
    public event Action? TransitionApplied;

    /// <summary>Binds inspector to a project and optional selected clip.</summary>
    public void Bind(MovieProject project, Guid? clipId)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _clip = clipId is { } id ? project.FindClip(id) : null;
        if (_clip is null)
        {
            _clipLabel.Text = "No clip selected.";
            IsEnabled = false;
            return;
        }

        var asset = project.FindAsset(_clip.AssetId);
        _clipLabel.Text = $"Clip: {asset?.Name ?? _clip.Id.ToString("N")[..8]} · {_clip.Duration.TotalSeconds:0.#}s";
        _kind.SelectedItem = _clip.OutTransition;
        _duration.Value = Math.Clamp(_clip.OutTransitionDuration.TotalSeconds, 0, 2);
        if (_duration.Value <= 0 && _clip.OutTransition != TransitionKind.None)
            _duration.Value = 0.5;
        IsEnabled = true;
        SyncDurationEnabled();
    }

    void SyncDurationEnabled()
    {
        var kind = _kind.SelectedItem is TransitionKind k ? k : TransitionKind.None;
        _duration.IsEnabled = kind != TransitionKind.None;
    }

    void Apply()
    {
        if (_clip is null || _project is null)
            return;
        var kind = _kind.SelectedItem is TransitionKind k ? k : TransitionKind.None;
        var seconds = kind == TransitionKind.None ? 0 : _duration.Value;
        MovieEditOps.SetOutTransition(_clip, kind, TimeSpan.FromSeconds(seconds));
        TransitionApplied?.Invoke();
    }
}
