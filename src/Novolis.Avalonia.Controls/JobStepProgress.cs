namespace Novolis.Avalonia.Controls;

/// <summary>Simple mutable <see cref="IJobStepProgress"/>.</summary>
public sealed class JobStepProgress : IJobStepProgress
{
    /// <inheritdoc />
    public required string Label { get; init; }

    /// <inheritdoc />
    public double Progress { get; set; }

    /// <inheritdoc />
    public string? StatusLabel { get; set; }
}
