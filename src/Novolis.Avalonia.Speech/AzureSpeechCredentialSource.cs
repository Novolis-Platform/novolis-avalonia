namespace Novolis.Avalonia.Speech;

/// <summary>How an Azure Speech connection was selected.</summary>
public enum AzureSpeechCredentialSource
{
    /// <summary>Selected after Microsoft Entra sign-in and resource discovery.</summary>
    Automatic,

    /// <summary>Loaded from a user-selected manual credentials file.</summary>
    Manual,
}
