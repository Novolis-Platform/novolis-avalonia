using Novolis.Visual.Definitions;

namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Raised when an exposed material control is changed in the inspector.
/// </summary>
public sealed class AssetStudioMaterialParameterChangedEventArgs : EventArgs
{
    public AssetStudioMaterialParameterChangedEventArgs(
        string parameterId,
        VisualValue value)
    {
        ParameterId = parameterId;
        Value = value;
    }

    public string ParameterId { get; }

    public VisualValue Value { get; }
}
