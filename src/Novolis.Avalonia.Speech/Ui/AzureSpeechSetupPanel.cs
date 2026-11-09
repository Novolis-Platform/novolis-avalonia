using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Profile = Novolis.Avalonia.GraphicalProfile.GraphicalProfile;

namespace Novolis.Avalonia.Speech;

/// <summary>
/// Interconnected Azure Speech setup: Entra login + resource cascade, or manual endpoint/key.
/// Bound to a shared <see cref="SpeechWorkspace"/>.
/// </summary>
public sealed class AzureSpeechSetupPanel : Border
{
    public static readonly StyledProperty<SpeechWorkspace?> WorkspaceProperty =
        AvaloniaProperty.Register<AzureSpeechSetupPanel, SpeechWorkspace?>(nameof(Workspace));

    readonly Button _automaticMode = MakeButton("Microsoft account");
    readonly Button _manualMode = MakeButton("Endpoint & key");
    readonly Button _signIn = MakeButton("Sign in with Microsoft");
    readonly Button _signOut = MakeButton("Sign out");
    readonly Button _saveManual = MakeButton("Save and connect");
    readonly Button _disconnect = MakeButton("Disconnect Azure");
    readonly Button _close = MakeButton("Close");
    readonly TextBlock _progress = new()
    {
        FontSize = 13,
        Foreground = Profile.MutedBrush,
        Text = "Step 1 of 3 — Sign in",
    };
    readonly TextBlock _resourceStatus = new()
    {
        FontSize = 13,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Profile.MutedBrush,
    };
    readonly ComboBox _subscriptionPicker = new() { MinHeight = 36, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly ComboBox _resourceGroupPicker = new() { MinHeight = 36, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly ComboBox _speechResourcePicker = new() { MinHeight = 36, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly TextBox _endpointBox = new()
    {
        PlaceholderText = "https://your-resource.cognitiveservices.azure.com/",
        MinHeight = 36,
    };
    readonly TextBox _keyBox = new()
    {
        PlaceholderText = "Subscription key",
        PasswordChar = '•',
        MinHeight = 36,
    };
    readonly StackPanel _automaticPanel;
    readonly StackPanel _manualPanel;
    readonly Grid _modeRow;
    SpeechWorkspace? _workspace;
    bool _suppress;

    public AzureSpeechSetupPanel()
    {
        Background = Profile.SurfaceBrush;
        BorderBrush = Profile.BorderBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(14);
        IsVisible = false;

        _automaticMode.Click += (_, _) => _workspace?.SetAzureCredentialMode(automatic: true);
        _manualMode.Click += (_, _) => _workspace?.SetAzureCredentialMode(automatic: false);
        _signIn.Click += async (_, _) =>
        {
            if (_workspace is null)
                return;
            try
            {
                await _workspace.SignInAzureAsync();
            }
            catch (Exception ex)
            {
                _resourceStatus.Text = ex.Message;
            }
        };
        _signOut.Click += async (_, _) =>
        {
            if (_workspace is null)
                return;
            await _workspace.DisconnectAzureAsync();
        };
        _saveManual.Click += async (_, _) => await SaveManualAsync();
        _disconnect.Click += async (_, _) =>
        {
            if (_workspace is null)
                return;
            await _workspace.DisconnectAzureAsync();
        };
        _close.Click += (_, _) => _workspace?.DismissAzureSetup();

        _subscriptionPicker.SelectionChanged += async (_, _) =>
        {
            if (_suppress || _workspace is null || _subscriptionPicker.SelectedItem is not AzureSubscriptionChoice sub)
                return;
            try
            {
                await _workspace.SelectSubscriptionAsync(sub.Id);
            }
            catch (Exception ex)
            {
                _resourceStatus.Text = ex.Message;
            }
        };
        _resourceGroupPicker.SelectionChanged += async (_, _) =>
        {
            if (_suppress || _workspace is null
                || _resourceGroupPicker.SelectedItem is not AzureSpeechResourceGroupChoice group)
                return;
            try
            {
                await _workspace.SelectResourceGroupAsync(group.Name);
            }
            catch (Exception ex)
            {
                _resourceStatus.Text = ex.Message;
            }
        };
        _speechResourcePicker.SelectionChanged += async (_, _) =>
        {
            if (_suppress || _workspace is null
                || _speechResourcePicker.SelectedItem is not AzureSpeechResourceChoice resource)
                return;
            try
            {
                await _workspace.ConnectDiscoveredResourceAsync(resource);
            }
            catch (Exception ex)
            {
                _resourceStatus.Text = ex.Message;
            }
        };

        _modeRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,*") };
        Grid.SetColumn(_automaticMode, 0);
        Grid.SetColumn(_manualMode, 2);
        _modeRow.Children.Add(_automaticMode);
        _modeRow.Children.Add(_manualMode);

        _automaticPanel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                _progress,
                new TextBlock
                {
                    Text = "No Speech key is stored. Microsoft keeps the sign-in on this device and looks up your tenant and Speech resources.",
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Profile.MutedBrush,
                },
                _signIn,
                _signOut,
                _resourceStatus,
                Labeled("Subscription", _subscriptionPicker),
                Labeled("Resource group", _resourceGroupPicker),
                Labeled("Speech resource", _speechResourcePicker),
            },
        };

        _manualPanel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Paste the Speech endpoint and subscription key. The key stays in platform secure storage.",
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Profile.MutedBrush,
                },
                Labeled("Endpoint", _endpointBox),
                Labeled("Subscription key", _keyBox),
                _saveManual,
                _disconnect,
            },
        };

        var header = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(_close, Dock.Right);
        header.Children.Add(_close);
        header.Children.Add(new TextBlock
        {
            Text = "Azure Speech connection",
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            Foreground = Profile.TextBrush,
            VerticalAlignment = VerticalAlignment.Center,
        });

        Child = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                header,
                new TextBlock
                {
                    Text = "Sign in with Microsoft to auto-lookup tenant and Speech resources, or enter an endpoint and key.",
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Profile.MutedBrush,
                },
                new TextBlock
                {
                    Text = "How to connect",
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Profile.TextBrush,
                },
                _modeRow,
                _automaticPanel,
                _manualPanel,
            },
        };
    }

    /// <summary>Shared speech session.</summary>
    public SpeechWorkspace? Workspace
    {
        get => GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != WorkspaceProperty)
            return;

        if (_workspace is not null)
            _workspace.Changed -= OnWorkspaceChanged;
        _workspace = Workspace;
        if (_workspace is not null)
            _workspace.Changed += OnWorkspaceChanged;
        Refresh();
    }

    void OnWorkspaceChanged(object? sender, EventArgs e) => Refresh();

    void Refresh()
    {
        if (_workspace is null)
        {
            IsVisible = false;
            return;
        }

        IsVisible = _workspace.WantAzureSetup
            || (_workspace.Provider == SpeechProvider.AzureSpeech && !_workspace.IsAzureConfigured);

        var canDiscover = _workspace.CanDiscoverAzure;
        _modeRow.IsVisible = canDiscover;
        var automatic = _workspace.PreferAutomaticAzure;
        _automaticPanel.IsVisible = canDiscover && automatic;
        _manualPanel.IsVisible = !canDiscover || !automatic;
        StyleSelected(_automaticMode, automatic);
        StyleSelected(_manualMode, !automatic);

        IsEnabled = !_workspace.IsBusy;
        _resourceStatus.Text = _workspace.StatusDetail;

        _suppress = true;
        try
        {
            _subscriptionPicker.ItemsSource = _workspace.Subscriptions;
            _subscriptionPicker.SelectedItem = _workspace.Subscriptions.FirstOrDefault(s =>
                string.Equals(s.Id, _workspace.SelectedSubscriptionId, StringComparison.OrdinalIgnoreCase));
            _resourceGroupPicker.ItemsSource = _workspace.ResourceGroups;
            _resourceGroupPicker.SelectedItem = _workspace.ResourceGroups.FirstOrDefault(g =>
                string.Equals(g.Name, _workspace.SelectedResourceGroup, StringComparison.OrdinalIgnoreCase));
            _speechResourcePicker.ItemsSource = _workspace.SpeechResources;

            _progress.Text = ResolveProgress(_workspace);
        }
        finally
        {
            _suppress = false;
        }

        if (_workspace.AzureConfiguration is { } setup
            && setup.EffectiveCredentialSource == AzureSpeechCredentialSource.Manual)
        {
            _endpointBox.Text = setup.Endpoint.ToString();
        }
    }

    async Task SaveManualAsync()
    {
        if (_workspace is null)
            return;

        if (!Uri.TryCreate(_endpointBox.Text?.Trim(), UriKind.Absolute, out var endpoint))
        {
            _resourceStatus.Text = "Enter a valid https endpoint.";
            return;
        }

        var key = _keyBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            _resourceStatus.Text = "Enter the subscription key.";
            return;
        }

        try
        {
            await _workspace.ConnectManualAsync(endpoint, key);
            _keyBox.Text = string.Empty;
        }
        catch (Exception ex)
        {
            _resourceStatus.Text = ex.Message;
        }
    }

    static string ResolveProgress(SpeechWorkspace workspace)
    {
        if (workspace.SpeechResources.Count > 0)
            return "Step 3 of 3 — Pick a Speech resource";
        if (workspace.ResourceGroups.Count > 0)
            return "Step 2 of 3 — Pick a resource group";
        if (workspace.Subscriptions.Count > 0)
            return "Step 2 of 3 — Pick a subscription";
        return "Step 1 of 3 — Sign in";
    }

    static StackPanel Labeled(string label, Control control) => new()
    {
        Spacing = 4,
        Children =
        {
            new TextBlock
            {
                Text = label,
                FontWeight = FontWeight.SemiBold,
                Foreground = Profile.TextBrush,
            },
            control,
        },
    };

    static Button MakeButton(string text) => new()
    {
        Content = text,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        Padding = new Thickness(10, 8),
        Margin = new Thickness(0, 0, 0, 0),
    };

    static void StyleSelected(Button button, bool selected)
    {
        button.Classes.Clear();
        if (selected)
            button.Classes.Add("ngp-action-button");
        button.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal;
    }
}
