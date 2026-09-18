using System.Text.Json;
using Azure;
using Azure.Identity;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.AzureSpeech;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Speech;

/// <summary>
/// Application speech front that selects local device playback or a user's
/// Azure Speech resource.
/// </summary>
public sealed class SpeechFront
{
    const string SecureStateKey = "speech.azure.state";
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    readonly IVoiceService _deviceVoice;
    readonly ISecureTokenStore _secureStore;
    readonly SemaphoreSlim _initialization = new(1, 1);
    AzureSpeechSetup? _azureSetup;
    AzureSpeechClient? _azureClient;
    SpeechProvider _provider = SpeechProvider.DeviceVoice;
    bool _initialized;

    /// <summary>Creates the service front from platform voice and secure storage services.</summary>
    public SpeechFront(IVoiceService deviceVoice, ISecureTokenStore secureStore)
    {
        _deviceVoice = deviceVoice ?? throw new ArgumentNullException(nameof(deviceVoice));
        _secureStore = secureStore ?? throw new ArgumentNullException(nameof(secureStore));
    }

    /// <summary>Raised after provider or Azure configuration changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The currently selected provider.</summary>
    public SpeechProvider Provider => _provider;

    /// <summary>Whether an Azure configuration is available.</summary>
    public bool IsAzureConfigured => _azureClient is not null && _azureSetup is not null;

    /// <summary>Redacted Azure configuration for displaying setup state.</summary>
    public AzureSpeechSetup? AzureConfiguration =>
        _azureSetup is null ? null : _azureSetup with { ApiKey = null };

    /// <summary>Capabilities of the current service front.</summary>
    public SpeechCapabilities Capabilities => new(
        CanDevicePlayback: true,
        CanAzurePlayback: IsAzureConfigured,
        CanCreateMp3: IsAzureConfigured);

    /// <summary>Loads encrypted user configuration once during application startup.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initialization.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            var json = await _secureStore
                .GetAsync(SecureStateKey, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var state = JsonSerializer.Deserialize<PersistedSpeechState>(json, JsonOptions);
                    if (state?.Azure is not null)
                    {
                        state.Azure.Validate();
                        _azureSetup = state.Azure;
                        _azureClient = CreateClient(state.Azure);
                        _provider = state.Provider == SpeechProvider.AzureSpeech
                            ? SpeechProvider.AzureSpeech
                            : SpeechProvider.DeviceVoice;
                    }
                }
                catch (JsonException)
                {
                    // A corrupt setting should not prevent local device speech.
                    await _secureStore.RemoveAsync(SecureStateKey, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (ArgumentException)
                {
                    // A stale setting should not prevent local device speech.
                    await _secureStore.RemoveAsync(SecureStateKey, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            _initialized = true;
        }
        finally
        {
            _initialization.Release();
        }
    }

    /// <summary>Switches to local device speech without network access.</summary>
    public async Task UseDeviceVoiceAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        _provider = SpeechProvider.DeviceVoice;
        await SaveStateAsync(cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Switches to Azure Speech, if a connection has been configured.</summary>
    public async Task UseAzureSpeechAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        EnsureAzureConfigured();
        _provider = SpeechProvider.AzureSpeech;
        await SaveStateAsync(cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Stores a user-owned Azure connection and selects Azure Speech. The key
    /// is held only in the platform secure store and in the in-memory SDK client.
    /// </summary>
    public async Task ConfigureAzureAsync(
        AzureSpeechSetup setup,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setup);
        setup.Validate();
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var client = CreateClient(setup);
        _azureSetup = setup;
        _azureClient = client;
        _provider = SpeechProvider.AzureSpeech;
        await SaveStateAsync(cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Tests Azure authentication by requesting the selected locale's voices.</summary>
    public async Task<IReadOnlyList<AzureSpeechVoice>> TestAzureAsync(
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        EnsureAzureConfigured();
        return await _azureClient!
            .GetVoicesAsync(_azureSetup!.Locale, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Removes the stored Azure configuration and returns to device speech.</summary>
    public async Task RemoveAzureAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        _azureSetup = null;
        _azureClient = null;
        _provider = SpeechProvider.DeviceVoice;
        await _secureStore.RemoveAsync(SecureStateKey, cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reads text through the selected provider. Azure playback is handed to
    /// the caller because only the host knows how to play MP3 bytes.
    /// </summary>
    public async Task ReadAsync(
        string text,
        Func<byte[], CancellationToken, Task> playAzureMp3Async,
        AzureSpeechSynthesisOptions? azureOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(playAzureMp3Async);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        if (_provider == SpeechProvider.DeviceVoice)
        {
            await _deviceVoice.SpeakAsync(text, cancellationToken).ConfigureAwait(false);
            return;
        }

        var mp3 = await CreateMp3Async(text, azureOptions, cancellationToken).ConfigureAwait(false);
        await playAzureMp3Async(mp3, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates MP3 bytes through Azure Speech.</summary>
    public async Task<byte[]> CreateMp3Async(
        string text,
        AzureSpeechSynthesisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        EnsureAzureConfigured();

        options ??= new AzureSpeechSynthesisOptions
        {
            VoiceName = _azureSetup!.VoiceName,
            Locale = _azureSetup.Locale,
        };
        return await _azureClient!
            .SynthesizeToMp3Async(text, options, cancellationToken)
            .ConfigureAwait(false);
    }

    AzureSpeechClient CreateClient(AzureSpeechSetup setup)
    {
        if (setup.AuthenticationMode == AzureSpeechAuthenticationMode.ApiKey)
        {
            return new AzureSpeechClient(
                setup.Endpoint,
                new AzureKeyCredential(setup.ApiKey!));
        }

        var credential = new InteractiveBrowserCredential(
            new InteractiveBrowserCredentialOptions
            {
                ClientId = setup.ClientId!,
                TenantId = setup.TenantId,
                RedirectUri = new Uri("http://localhost"),
            });
        return new AzureSpeechClient(setup.Endpoint, credential);
    }

    async Task SaveStateAsync(CancellationToken cancellationToken)
    {
        var state = new PersistedSpeechState(_provider, _azureSetup);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await _secureStore.SetAsync(SecureStateKey, json, cancellationToken).ConfigureAwait(false);
    }

    void EnsureAzureConfigured()
    {
        if (!IsAzureConfigured)
        {
            throw new SpeechCapabilityException(
                "Azure Speech is not configured. Add your own Speech endpoint and credentials first.");
        }
    }

    sealed record PersistedSpeechState(
        SpeechProvider Provider,
        AzureSpeechSetup? Azure);
}

/// <summary>Dependency-injection registration for the application speech front.</summary>
public static class SpeechServiceCollectionExtensions
{
    /// <summary>Registers <see cref="SpeechFront"/>.</summary>
    public static IServiceCollection AddNovolisSpeech(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<SpeechFront>();
        return services;
    }
}
