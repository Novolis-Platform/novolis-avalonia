using Azure.Core;
using Azure.Identity;

namespace Novolis.Avalonia.Speech;

/// <summary>
/// Default desktop credential factory. Mobile hosts can register a native
/// public-client implementation through dependency injection.
/// </summary>
internal sealed class AzureIdentitySpeechCredentialFactory : IAzureSpeechCredentialFactory
{
    public TokenCredential Create(AzureSpeechSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        return new InteractiveBrowserCredential(
            new InteractiveBrowserCredentialOptions
            {
                ClientId = setup.ClientId!,
                TenantId = setup.TenantId,
                RedirectUri = new Uri("http://localhost"),
            });
    }
}
