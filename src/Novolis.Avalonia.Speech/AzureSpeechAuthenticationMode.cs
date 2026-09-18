namespace Novolis.Avalonia.Speech;

/// <summary>Credential mode used for the user's Azure Speech resource.</summary>
public enum AzureSpeechAuthenticationMode
{
    /// <summary>Use an Azure Speech subscription key.</summary>
    ApiKey,

    /// <summary>Use an interactive Microsoft Entra sign-in.</summary>
    MicrosoftEntra,
}
