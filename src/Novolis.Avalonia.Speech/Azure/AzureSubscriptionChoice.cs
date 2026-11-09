namespace Novolis.Avalonia.Speech;

/// <summary>A subscription visible to the signed-in Azure account.</summary>
public sealed record AzureSubscriptionChoice(
    string Id,
    string DisplayName)
{
    /// <inheritdoc />
    public override string ToString() =>
        string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;
}
