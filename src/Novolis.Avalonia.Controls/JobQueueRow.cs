namespace Novolis.Avalonia.Controls;

/// <summary>Simple mutable <see cref="IJobQueueRow"/> for demos and adapters.</summary>
public sealed class JobQueueRow : IJobQueueRow
{
    /// <inheritdoc />
    public required string Title { get; init; }

    /// <inheritdoc />
    public required string StatusLabel { get; set; }

    /// <inheritdoc />
    public string? Detail { get; set; }

    /// <inheritdoc />
    public string? LogTail { get; set; }

    /// <inheritdoc />
    public bool CanCancel { get; set; }

    /// <inheritdoc />
    public bool CanOpenOutput { get; set; }

    /// <inheritdoc />
    public double? Progress { get; set; }

    /// <inheritdoc />
    public string? ProgressLabel { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IJobStepProgress>? StepProgress { get; set; }

    /// <inheritdoc />
    public object? Tag { get; init; }
}
