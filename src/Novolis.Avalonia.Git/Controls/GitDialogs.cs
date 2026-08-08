using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Novolis.IO.Git;

namespace Novolis.Avalonia.Git;

/// <summary>Single-repo create-branch dialog content.</summary>
public sealed class GitCreateBranchDialog : UserControl
{
    readonly TextBox _name = new() { PlaceholderText = "feat/..." };
    readonly TextBox _base = new() { Text = "main", PlaceholderText = "base ref" };
    readonly CheckBox _checkout = new() { Content = "Checkout", IsChecked = true };

    /// <summary>Creates dialog body.</summary>
    public GitCreateBranchDialog()
    {
        Content = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(12),
            Children =
            {
                new TextBlock { Text = "Create branch" },
                _name,
                _base,
                _checkout,
            },
        };
    }

    /// <summary>Reads options.</summary>
    public CreateBranchOptions? TryRead()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
            return null;
        return new CreateBranchOptions
        {
            Name = _name.Text.Trim(),
            BaseRef = string.IsNullOrWhiteSpace(_base.Text) ? "main" : _base.Text.Trim(),
            Checkout = _checkout.IsChecked == true,
        };
    }
}

/// <summary>Multi-repo branch-cut dialog content with dry-run preview.</summary>
public sealed class GitBranchCutDialog : UserControl
{
    readonly TextBox _name = new() { PlaceholderText = "feat/..." };
    readonly TextBox _base = new() { Text = "main" };
    readonly TextBox _preview = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        Height = 180,
        TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
    };

    /// <summary>Creates dialog body.</summary>
    public GitBranchCutDialog()
    {
        Content = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(12),
            Width = 480,
            Children =
            {
                new TextBlock { Text = "Feature branch cut (multi-repo)" },
                _name,
                _base,
                new TextBlock { Text = "Dry-run preview" },
                _preview,
            },
        };
    }

    /// <summary>Branch name.</summary>
    public string BranchName => _name.Text?.Trim() ?? "";

    /// <summary>Base ref.</summary>
    public string BaseRef => string.IsNullOrWhiteSpace(_base.Text) ? "main" : _base.Text.Trim();

    /// <summary>Shows plan preview text.</summary>
    public void SetPreview(BranchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var lines = plan.Steps.Select(s =>
            s.BlockReason is null
                ? $"OK  {s.Repo.Name}  {string.Join(' ', s.PlannedArgs)}"
                : $"SKIP {s.Repo.Name}  {s.BlockReason}");
        _preview.Text = string.Join('\n', lines);
    }
}
