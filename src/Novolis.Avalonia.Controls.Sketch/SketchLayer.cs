namespace Novolis.Avalonia.Controls.Sketch;

/// <summary>Named layer for z-order grouping and visibility / lock.</summary>
public sealed class SketchLayer
{
    /// <summary>Stable layer id.</summary>
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "Layer";

    /// <summary>When false, elements on this layer are not drawn or hit-tested.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>When true, elements on this layer cannot be edited or filled.</summary>
    public bool Locked { get; set; }

    /// <summary>Deep-clones this layer.</summary>
    public SketchLayer Clone() => new()
    {
        Id = Id,
        Name = Name,
        Visible = Visible,
        Locked = Locked
    };
}
