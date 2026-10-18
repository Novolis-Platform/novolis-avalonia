namespace Novolis.Avalonia.AssetStudio;

/// <summary>Reports a material module assignment made in an asset inspector.</summary>
public sealed class AssetStudioMaterialSlotChangedEventArgs : EventArgs
{
    public AssetStudioMaterialSlotChangedEventArgs(
        string slotId,
        string materialId)
    {
        SlotId = string.IsNullOrWhiteSpace(slotId)
            ? throw new ArgumentException(
                "A slot identifier is required.",
                nameof(slotId))
            : slotId;
        MaterialId = string.IsNullOrWhiteSpace(materialId)
            ? throw new ArgumentException(
                "A material identifier is required.",
                nameof(materialId))
            : materialId;
    }

    public string SlotId { get; }

    public string MaterialId { get; }
}
