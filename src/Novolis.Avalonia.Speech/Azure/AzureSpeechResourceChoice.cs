namespace Novolis.Avalonia.Speech;

/// <summary>A selectable Azure Speech resource discovered after Entra sign-in.</summary>
public sealed record AzureSpeechResourceChoice(
    string SubscriptionId,
    string ResourceGroupName,
    string Name,
    Uri Endpoint,
    string Location,
    string ClientId,
    string TenantId)
{
    /// <inheritdoc />
    public override string ToString() =>
        string.IsNullOrWhiteSpace(Location) ? Name : $"{Name} · {Location}";
}
