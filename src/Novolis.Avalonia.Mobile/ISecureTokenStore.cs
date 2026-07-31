namespace Novolis.Avalonia.Mobile;

/// <summary>Platform-backed secret storage for OAuth access tokens.</summary>
public interface ISecureTokenStore
{
    /// <summary>Reads a secret by key, or <see langword="null"/> if missing.</summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Writes or replaces a secret.</summary>
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>Removes a secret if present.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Well-known token store keys used by Novolis mobile apps.</summary>
public static class SecureTokenKeys
{
    /// <summary>GitHub OAuth access token.</summary>
    public const string GitHubOAuthAccessToken = "github.oauth.access_token";
}
