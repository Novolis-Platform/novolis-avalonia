using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Carries one no-code asset control edit from the inspector.
/// </summary>
public sealed class AssetStudioAssetParameterChangedEventArgs : EventArgs
{
    public AssetStudioAssetParameterChangedEventArgs(
        string parameterId,
        VisualValue value)
    {
        if (string.IsNullOrWhiteSpace(parameterId))
        {
            throw new ArgumentException(
                "An asset parameter identifier is required.",
                nameof(parameterId));
        }

        ArgumentNullException.ThrowIfNull(value);
        ParameterId = parameterId.Trim();
        Value = value;
    }

    public string ParameterId { get; }

    public VisualValue Value { get; }
}
