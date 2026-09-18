namespace Novolis.Avalonia.Speech;

/// <summary>
/// Azure Speech connection details supplied by the user. The API key is only
/// accepted for the duration of setup and is persisted by <see cref="SpeechFront"/>
/// through the platform secure store.
/// </summary>
public sealed record AzureSpeechSetup
{
    /// <summary>Azure Speech resource endpoint.</summary>
    public required Uri Endpoint { get; init; }

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
