using Azure.Core;
using Azure.Identity;

namespace Novolis.Avalonia.Speech;

/// <summary>Creates Microsoft Entra credentials for Azure Speech connections.</summary>
public interface IAzureSpeechCredentialFactory
{
    /// <summary>Creates a credential for the supplied user-owned Speech setup.</summary>
    TokenCredential Create(AzureSpeechSetup setup);
}
