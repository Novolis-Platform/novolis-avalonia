namespace Novolis.Avalonia.Speech;

/// <summary>
/// Azure Speech connection details supplied by the user. The API key is used
/// only for a manually configured connection and is persisted by
/// <see cref="SpeechFront"/> through the platform secure store.
/// </summary>
public sealed record AzureSpeechSetup
{
    /// <summary>Azure Speech resource endpoint.</summary>
    public required Uri Endpoint { get; init; }

    /// <summary>Whether the connection came from automatic discovery or manual import.</summary>
    /// <remarks>
    /// Older saved configurations do not contain this property. Those configurations
    /// are interpreted from <see cref="AuthenticationMode"/> by <see cref="EffectiveCredentialSource"/>.
    /// </remarks>
    public AzureSpeechCredentialSource? CredentialSource { get; init; }

    /// <summary>Credential mode.</summary>
    public AzureSpeechAuthenticationMode AuthenticationMode { get; init; } =
        AzureSpeechAuthenticationMode.ApiKey;

    /// <summary>Subscription key for <see cref="AzureSpeechAuthenticationMode.ApiKey"/>.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Registered application client id for Entra sign-in.</summary>
    public string? ClientId { get; init; }

    /// <summary>Optional tenant id for Entra sign-in.</summary>
    public string? TenantId { get; init; }

    /// <summary>Default Azure Speech voice.</summary>
    public string VoiceName { get; init; } = "en-US-AvaMultilingualNeural";

    /// <summary>Default SSML locale.</summary>
    public string Locale { get; init; } = "en-US";

    /// <summary>Returns the source, including a compatibility result for older saved state.</summary>
    public AzureSpeechCredentialSource EffectiveCredentialSource =>
        CredentialSource
        ?? (AuthenticationMode == AzureSpeechAuthenticationMode.MicrosoftEntra
            ? AzureSpeechCredentialSource.Automatic
            : AzureSpeechCredentialSource.Manual);

    /// <summary>Validates user-supplied connection details.</summary>
    public void Validate()
    {
        if (!Endpoint.IsAbsoluteUri ||
            !string.Equals(Endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Azure Speech endpoint must be an absolute HTTPS URI.", nameof(Endpoint));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(VoiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(Locale);

        if (AuthenticationMode == AzureSpeechAuthenticationMode.ApiKey)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ApiKey);
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ClientId);
    }
}
