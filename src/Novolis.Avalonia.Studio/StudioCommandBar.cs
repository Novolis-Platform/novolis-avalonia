using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Novolis.Avalonia.Studio;

/// <summary>
/// Studio command prompt: label + text box with history, submit, and escape-cancel.
/// CAD-/domain-agnostic — raises events only.
/// </summary>
public sealed class StudioCommandBar : UserControl
{
    private readonly TextBlock _promptLabel;
    private readonly TextBox _input;
    private readonly List<string> _history = [];
    private int _historyIndex = -1;
    private string _draft = string.Empty;

    /// <summary>Creates a command bar with an optional initial prompt label.</summary>
    public StudioCommandBar(string promptLabel = "Command:")
    {
        _promptLabel = new TextBlock
        {
            Text = promptLabel,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Opacity = 0.85,
        };

        _input = new TextBox
        {
            PlaceholderText = "Type a command…",
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _input.KeyDown += OnInputKeyDown;

        var row = new DockPanel { Margin = new Thickness(8, 4) };
        DockPanel.SetDock(_promptLabel, Dock.Left);
        row.Children.Add(_promptLabel);
        row.Children.Add(_input);

        Content = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
            Child = row,
        };
    }

    /// <summary>Prompt label shown to the left of the input (e.g. tool mode hints).</summary>
    public string PromptLabel
    {
        get => _promptLabel.Text ?? string.Empty;
        set => _promptLabel.Text = value;
    }

    /// <summary>Current input text.</summary>
    public string Text
    {
        get => _input.Text ?? string.Empty;
        set => _input.Text = value;
    }

    /// <summary>Raised when the user submits a non-empty prompt (Enter).</summary>
    public event EventHandler<StudioCommandSubmittedEventArgs>? Submitted;

    /// <summary>Raised when the user cancels (Escape) with an empty or non-empty field.</summary>
    public event EventHandler? Cancelled;

    /// <summary>Moves keyboard focus into the command input.</summary>
    public void FocusInput() => _input.Focus();

    /// <summary>Clears the input and optional draft tracking.</summary>
    public void Clear()
    {
        _input.Text = string.Empty;
        _draft = string.Empty;
        _historyIndex = -1;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = (_input.Text ?? string.Empty).Trim();
            if (text.Length > 0)
            {
                if (_history.Count == 0 || !string.Equals(_history[^1], text, StringComparison.Ordinal))
                    _history.Add(text);
                _historyIndex = -1;
                _draft = string.Empty;
                _input.Text = string.Empty;
                Submitted?.Invoke(this, new StudioCommandSubmittedEventArgs(text));
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            NavigateHistory(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            NavigateHistory(+1);
            e.Handled = true;
        }
    }

    private void NavigateHistory(int delta)
    {
        if (_history.Count == 0)
            return;

        if (_historyIndex < 0)
            _draft = _input.Text ?? string.Empty;

        var next = _historyIndex < 0
            ? _history.Count - 1
            : _historyIndex + delta;

        if (next < 0)
            next = 0;
        if (next >= _history.Count)
        {
            _historyIndex = -1;
            _input.Text = _draft;
            _input.CaretIndex = _input.Text?.Length ?? 0;
            return;
        }

        _historyIndex = next;
        _input.Text = _history[next];
        _input.CaretIndex = _input.Text?.Length ?? 0;
    }
}

/// <summary>Payload for <see cref="StudioCommandBar.Submitted"/>.</summary>
public sealed class StudioCommandSubmittedEventArgs : EventArgs
{
    /// <summary>Creates event args with the submitted prompt text.</summary>
    public StudioCommandSubmittedEventArgs(string text) => Text = text;

    /// <summary>Trimmed prompt text.</summary>
    public string Text { get; }
}
