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
