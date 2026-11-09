namespace Novolis.Avalonia.Speech;

/// <summary>A resource group containing at least one compatible Speech resource.</summary>
public sealed record AzureSpeechResourceGroupChoice(
    string Name,
    int SpeechResourceCount)
{
    /// <inheritdoc />
    public override string ToString() =>
        SpeechResourceCount <= 0 ? Name : $"{Name} ({SpeechResourceCount})";
}
