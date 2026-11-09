namespace Novolis.Avalonia.Speech;

/// <summary>
/// Discovers user-visible Azure Speech resources after interactive sign-in.
/// Platform hosts provide the implementation because authentication and browser
/// callbacks are platform-specific.
/// </summary>
public interface IAzureSpeechResourcePicker
{
    /// <summary>Signs in and returns enabled subscriptions visible to the account.</summary>
    Task<IReadOnlyList<AzureSubscriptionChoice>> SignInAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns only groups containing compatible Speech resources.</summary>
    Task<IReadOnlyList<AzureSpeechResourceGroupChoice>> GetSpeechResourceGroupsAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns compatible Speech resources in the selected group.</summary>
    Task<IReadOnlyList<AzureSpeechResourceChoice>> GetSpeechResourcesAsync(
        string subscriptionId,
        string resourceGroupName,
        CancellationToken cancellationToken = default);

    /// <summary>Signs out and removes the platform token cache.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);
}
