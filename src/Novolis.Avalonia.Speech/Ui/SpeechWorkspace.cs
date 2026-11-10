using Avalonia.Threading;
using Novolis.Audio.Voice.AzureSpeech;
using Novolis.Audio.Voice.EdgeTts;
using Novolis.Manuscript.Export.Audio;

namespace Novolis.Avalonia.Speech;

/// <summary>
/// Shared speech session for interconnected Avalonia controls.
/// Default provider is Edge Read Aloud; Azure is optional and may use Entra lookup.
/// </summary>
public sealed class SpeechWorkspace
{
    readonly SpeechFront _front;
    readonly IAzureSpeechResourcePicker? _resourcePicker;
    readonly List<AzureSubscriptionChoice> _subscriptions = [];
    readonly List<AzureSpeechResourceGroupChoice> _resourceGroups = [];
    readonly List<AzureSpeechResourceChoice> _speechResources = [];
    readonly List<SpeechVoiceChoice> _voices = [];

    string? _selectedSubscriptionId;
    string? _selectedResourceGroup;
    string? _statusMessage;
    bool _busy;
    bool _preferAutomaticAzure;

    /// <summary>Creates a workspace over <paramref name="front"/>.</summary>
    public SpeechWorkspace(
        SpeechFront front,
        IAzureSpeechResourcePicker? resourcePicker = null)
    {
        _front = front ?? throw new ArgumentNullException(nameof(front));
        _resourcePicker = resourcePicker;
        _preferAutomaticAzure = resourcePicker is not null;
        Voice = new VoiceSettings
        {
            Voice = EdgeVoiceCatalog.ToShortName(EdgeVoice.EnUsAva),
            RatePercent = -4,
        };
        RebuildVoicesForProvider();
    }

    /// <summary>Raised after any UI-relevant state change.</summary>
    public event EventHandler? Changed;

    /// <summary>Underlying speech front.</summary>
    public SpeechFront Front => _front;

    /// <summary>Optional Azure resource discovery (Entra login).</summary>
    public IAzureSpeechResourcePicker? ResourcePicker => _resourcePicker;

    /// <summary>Whether automatic Azure discovery is available.</summary>
    public bool CanDiscoverAzure => _resourcePicker is not null;

    /// <summary>Current provider.</summary>
    public SpeechProvider Provider => _front.Provider;

    /// <summary>Whether Azure credentials are configured.</summary>
    public bool IsAzureConfigured => _front.IsAzureConfigured;

    /// <summary>Redacted Azure setup.</summary>
    public AzureSpeechSetup? AzureConfiguration => _front.AzureConfiguration;

    /// <summary>Capabilities of the current provider.</summary>
    public SpeechCapabilities Capabilities => _front.Capabilities;

    /// <summary>In-memory voice settings used for synthesis requests.</summary>
    public VoiceSettings Voice { get; private set; }

    /// <summary>Available voices for the current provider.</summary>
    public IReadOnlyList<SpeechVoiceChoice> Voices => _voices;

    /// <summary>Discovered Azure subscriptions after sign-in.</summary>
    public IReadOnlyList<AzureSubscriptionChoice> Subscriptions => _subscriptions;

    /// <summary>Speech resource groups for the selected subscription.</summary>
    public IReadOnlyList<AzureSpeechResourceGroupChoice> ResourceGroups => _resourceGroups;

    /// <summary>Speech resources for the selected group.</summary>
    public IReadOnlyList<AzureSpeechResourceChoice> SpeechResources => _speechResources;

    /// <summary>Selected Azure subscription id.</summary>
    public string? SelectedSubscriptionId => _selectedSubscriptionId;

    /// <summary>Selected Azure resource group name.</summary>
    public string? SelectedResourceGroup => _selectedResourceGroup;

    /// <summary>Prefer Entra discovery UI when a picker is registered.</summary>
    public bool PreferAutomaticAzure
    {
        get => _preferAutomaticAzure && CanDiscoverAzure;
        private set => _preferAutomaticAzure = value;
    }

    /// <summary>True while an async UI operation is running.</summary>
    public bool IsBusy => _busy;

    /// <summary>Short status line for chrome.</summary>
    public string StatusTitle => Provider switch
    {
        SpeechProvider.EdgeTts => "Edge Read Aloud",
        SpeechProvider.AzureSpeech when IsAzureConfigured => "Azure Speech ready",
        SpeechProvider.AzureSpeech => "Connect Azure Speech",
        SpeechProvider.DeviceVoice => "Device voice",
        _ => "Speech",
    };

    /// <summary>Supporting status detail.</summary>
    public string StatusDetail
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_statusMessage))
                return _statusMessage!;

            return Provider switch
            {
                SpeechProvider.EdgeTts =>
                    "Online MP3 synthesis · no Azure credentials required.",
                SpeechProvider.AzureSpeech when AzureConfiguration is { } setup =>
                    setup.EffectiveCredentialSource == AzureSpeechCredentialSource.Manual
                        ? $"{setup.Endpoint.Host} · saved subscription key"
                        : $"{setup.Endpoint.Host} · Microsoft account",
                SpeechProvider.AzureSpeech =>
                    CanDiscoverAzure
                        ? "Sign in with Microsoft to pick a Speech resource, or enter an endpoint and key."
                        : "Enter a Speech endpoint and subscription key.",
                SpeechProvider.DeviceVoice =>
                    "Local playback only · cannot create MP3 files.",
                _ => string.Empty,
            };
        }
    }

    /// <summary>
    /// Loads persisted speech state and selects Edge when nothing else is configured.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _front.InitializeAsync(cancellationToken).ConfigureAwait(false);

        if (_front.IsAzureConfigured)
        {
            if (_front.Provider != SpeechProvider.AzureSpeech)
                await _front.UseAzureSpeechAsync(cancellationToken).ConfigureAwait(false);

            var setup = _front.AzureConfiguration;
            PreferAutomaticAzure =
                CanDiscoverAzure
                && setup?.EffectiveCredentialSource == AzureSpeechCredentialSource.Automatic;
            if (setup is not null && !string.IsNullOrWhiteSpace(setup.VoiceName))
                SetVoice(setup.VoiceName);
        }
        else if (_front.Provider != SpeechProvider.EdgeTts)
        {
            await _front.UseEdgeTtsAsync(cancellationToken).ConfigureAwait(false);
        }

        RebuildVoicesForProvider();
        Notify();
    }

    /// <summary>Selects Edge Read Aloud (default MP3 path).</summary>
    public async Task SelectEdgeAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            WantAzureSetup = false;
            await _front.UseEdgeTtsAsync(cancellationToken).ConfigureAwait(false);
            RebuildVoicesForProvider();
            SetStatus("Using Edge Read Aloud.");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Selects local device voice (playback only).</summary>
    public async Task SelectDeviceAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            WantAzureSetup = false;
            await _front.UseDeviceVoiceAsync(cancellationToken).ConfigureAwait(false);
            RebuildVoicesForProvider();
            SetStatus("Using device voice.");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Selects Azure Speech when configured; otherwise opens Azure setup intent.</summary>
    public async Task SelectAzureAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            if (_front.IsAzureConfigured)
            {
                await _front.UseAzureSpeechAsync(cancellationToken).ConfigureAwait(false);
                RebuildVoicesForProvider();
                SetStatus("Using Azure Speech.");
                return;
            }

            // Stay on Edge until credentials exist, but surface Azure setup UI.
            PreferAutomaticAzure = CanDiscoverAzure;
            SetStatus(CanDiscoverAzure
                ? "Sign in with Microsoft to connect Azure Speech."
                : "Enter an Azure Speech endpoint and key.");
        }, cancellationToken).ConfigureAwait(false);
        // Force Azure panel visibility via a transient “want azure” flag on detail.
        WantAzureSetup = true;
        Notify();
    }

    /// <summary>True when the host should show the Azure setup panel.</summary>
    public bool WantAzureSetup { get; private set; }

    /// <summary>Hides the Azure setup panel without changing the provider.</summary>
    public void DismissAzureSetup()
    {
        WantAzureSetup = false;
        Notify();
    }

    /// <summary>Shows automatic (Entra) vs manual credential UI.</summary>
    public void SetAzureCredentialMode(bool automatic)
    {
        PreferAutomaticAzure = automatic && CanDiscoverAzure;
        WantAzureSetup = true;
        Notify();
    }

    /// <summary>Interactive Microsoft sign-in and subscription list.</summary>
    public async Task SignInAzureAsync(CancellationToken cancellationToken = default)
    {
        if (_resourcePicker is null)
            throw new SpeechCapabilityException("Azure resource discovery is not available on this host.");

        string? autoSubscriptionId = null;
        await RunBusyAsync(async () =>
        {
            WantAzureSetup = true;
            PreferAutomaticAzure = true;
            _subscriptions.Clear();
            _resourceGroups.Clear();
            _speechResources.Clear();
            _selectedSubscriptionId = null;
            _selectedResourceGroup = null;
            var items = await _resourcePicker.SignInAsync(cancellationToken).ConfigureAwait(false);
            _subscriptions.AddRange(items);
            if (items.Count == 1)
                autoSubscriptionId = items[0].Id;
            SetStatus(items.Count == 0
                ? "Signed in, but no enabled subscriptions were returned for this account."
                : items.Count == 1
                    ? $"Signed in · loading Speech resources for {items[0].DisplayName}…"
                    : $"Signed in · {items.Count} subscription(s). Pick one below.");
        }, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(autoSubscriptionId))
            await SelectSubscriptionAsync(autoSubscriptionId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads Speech resource groups for a subscription.</summary>
    public async Task SelectSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        if (_resourcePicker is null)
            throw new SpeechCapabilityException("Azure resource discovery is not available on this host.");

        // Commit selection before busy Notify so Refresh does not clear the ComboBox.
        _selectedSubscriptionId = subscriptionId;
        _selectedResourceGroup = null;
        _resourceGroups.Clear();
        _speechResources.Clear();
        SetStatus("Looking up Speech resource groups…");
        Notify();

        string? autoGroup = null;
        await RunBusyAsync(async () =>
        {
            var groups = await _resourcePicker
                .GetSpeechResourceGroupsAsync(subscriptionId, cancellationToken)
                .ConfigureAwait(false);
            _resourceGroups.Clear();
            _resourceGroups.AddRange(groups);
            if (groups.Count == 1)
                autoGroup = groups[0].Name;
            SetStatus(groups.Count == 0
                ? "No Speech resource groups in that subscription."
                : groups.Count == 1
                    ? $"Found 1 resource group · loading Speech resources…"
                    : $"Found {groups.Count} resource group(s) with Speech. Pick one below.");
        }, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(autoGroup))
            await SelectResourceGroupAsync(autoGroup, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads Speech resources in a resource group.</summary>
    public async Task SelectResourceGroupAsync(
        string resourceGroupName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceGroupName);
        if (_resourcePicker is null || string.IsNullOrWhiteSpace(_selectedSubscriptionId))
            throw new SpeechCapabilityException("Select a subscription before a resource group.");

        _selectedResourceGroup = resourceGroupName;
        _speechResources.Clear();
        SetStatus("Looking up Speech resources…");
        Notify();

        await RunBusyAsync(async () =>
        {
            var resources = await _resourcePicker
                .GetSpeechResourcesAsync(_selectedSubscriptionId, resourceGroupName, cancellationToken)
                .ConfigureAwait(false);
            _speechResources.Clear();
            _speechResources.AddRange(resources);
            SetStatus(resources.Count == 0
                ? "No Speech resources in that group."
                : $"Found {resources.Count} Speech resource(s). Pick one below.");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Configures Azure from a discovered Speech resource and selects it.</summary>
    public async Task ConnectDiscoveredResourceAsync(
        AzureSpeechResourceChoice resource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        await RunBusyAsync(async () =>
        {
            var setup = new AzureSpeechSetup
            {
                Endpoint = resource.Endpoint,
                CredentialSource = AzureSpeechCredentialSource.Automatic,
                AuthenticationMode = AzureSpeechAuthenticationMode.MicrosoftEntra,
                ClientId = resource.ClientId,
                TenantId = resource.TenantId,
                VoiceName = Voice.Voice,
                Locale = "en-US",
            };
            await _front.ConfigureAzureAsync(setup, cancellationToken).ConfigureAwait(false);
            WantAzureSetup = false;
            PreferAutomaticAzure = true;
            RebuildVoicesForProvider();
            await RefreshAzureVoicesAsync(cancellationToken).ConfigureAwait(false);
            SetStatus($"Connected {resource.Name}.");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Configures Azure from a manual endpoint and key.</summary>
    public async Task ConnectManualAsync(
        Uri endpoint,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        await RunBusyAsync(async () =>
        {
            var setup = new AzureSpeechSetup
            {
                Endpoint = endpoint,
                ApiKey = apiKey.Trim(),
                CredentialSource = AzureSpeechCredentialSource.Manual,
                AuthenticationMode = AzureSpeechAuthenticationMode.ApiKey,
                VoiceName = Voice.Voice,
                Locale = "en-US",
            };
            await _front.ConfigureAzureAsync(setup, cancellationToken).ConfigureAwait(false);
            WantAzureSetup = false;
            PreferAutomaticAzure = false;
            RebuildVoicesForProvider();
            await RefreshAzureVoicesAsync(cancellationToken).ConfigureAwait(false);
            SetStatus($"Connected {endpoint.Host}.");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes Azure credentials and returns to Edge.</summary>
    public async Task DisconnectAzureAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            if (_resourcePicker is not null)
            {
                try
                {
                    await _resourcePicker.SignOutAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Local disconnect should succeed even if remote sign-out fails.
                }
            }

            await _front.RemoveAzureAsync(cancellationToken).ConfigureAwait(false);
            _subscriptions.Clear();
            _resourceGroups.Clear();
            _speechResources.Clear();
            _selectedSubscriptionId = null;
            _selectedResourceGroup = null;
            WantAzureSetup = false;
            RebuildVoicesForProvider();
            SetStatus("Azure disconnected · Edge Read Aloud selected.");
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates the in-memory voice and persists it on Azure when configured.</summary>
    public async Task SetVoiceAsync(
        string voiceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(voiceName);
        SetVoice(voiceName);
        if (_front.IsAzureConfigured && _front.Provider == SpeechProvider.AzureSpeech)
        {
            await _front.UpdateAzureVoiceAsync(Voice.Voice, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        Notify();
    }

    /// <summary>Creates MP3 bytes with the current provider and voice settings.</summary>
    public Task<byte[]> CreateMp3Async(
        string text,
        CancellationToken cancellationToken = default)
    {
        var options = new AzureSpeechSynthesisOptions
        {
            VoiceName = Voice.Voice,
            Locale = _front.AzureConfiguration?.Locale ?? "en-US",
            RatePercent = Voice.RatePercent,
            PitchHertz = Voice.PitchHertz,
            VolumePercent = Voice.VolumePercent,
        };
        return _front.CreateMp3Async(text, options, cancellationToken);
    }

    void SetVoice(string voiceName)
    {
        Voice = new VoiceSettings
        {
            Voice = voiceName.Trim(),
            RatePercent = Voice.RatePercent,
            PitchHertz = Voice.PitchHertz,
            VolumePercent = Voice.VolumePercent,
            SceneBreakMs = Voice.SceneBreakMs,
            PauseMs = Voice.PauseMs,
            MaxChunkChars = Voice.MaxChunkChars,
            Pronunciation = Voice.Pronunciation,
        };
    }

    async Task RefreshAzureVoicesAsync(CancellationToken cancellationToken)
    {
        if (!_front.IsAzureConfigured)
            return;

        try
        {
            var azureVoices = await _front.ListVoicesAsync(cancellationToken).ConfigureAwait(false);
            _voices.Clear();
            foreach (var voice in azureVoices
                         .OrderBy(v => v.ShortName, StringComparer.OrdinalIgnoreCase))
            {
                _voices.Add(new SpeechVoiceChoice(
                    voice.ShortName,
                    string.IsNullOrWhiteSpace(voice.LocalName)
                        ? voice.ShortName
                        : $"{voice.LocalName} ({voice.ShortName})",
                    voice.Locale));
            }

            if (_voices.Count == 0)
                RebuildVoicesForProvider();
        }
        catch
        {
            RebuildVoicesForProvider();
        }
    }

    void RebuildVoicesForProvider()
    {
        _voices.Clear();
        if (Provider == SpeechProvider.EdgeTts || !IsAzureConfigured)
        {
            foreach (var entry in EdgeVoiceCatalog.All)
            {
                _voices.Add(new SpeechVoiceChoice(
                    entry.ShortName,
                    $"{entry.DisplayName} · Edge",
                    entry.Locale));
            }
        }
        else
        {
            // Placeholder until Azure voice list loads.
            _voices.Add(new SpeechVoiceChoice(
                Voice.Voice,
                Voice.Voice,
                _front.AzureConfiguration?.Locale ?? "en-US"));
        }
    }

    void SetStatus(string message) => _statusMessage = message;

    async Task RunBusyAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        _busy = true;
        Notify();
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _busy = false;
            Notify();
        }
    }

    void Notify()
    {
        // Sign-in / ARM lookups resume on a thread-pool thread after ConfigureAwait(false).
        // Avalonia controls listening to Changed must only refresh on the UI thread.
        if (Dispatcher.UIThread.CheckAccess())
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        Dispatcher.UIThread.Post(() => Changed?.Invoke(this, EventArgs.Empty));
    }
}
