namespace Novolis.Avalonia.Controls;

/// <summary>One row in a <see cref="JobQueuePanel"/> (domain-agnostic).</summary>
public interface IJobQueueRow
{
    /// <summary>Display title.</summary>
    string Title { get; }

    /// <summary>Short status label.</summary>
    string StatusLabel { get; }

    /// <summary>Optional detail line.</summary>
    string? Detail { get; }

    /// <summary>Optional log tail for the selected job.</summary>
    string? LogTail { get; }

    /// <summary>Whether Cancel is enabled.</summary>
    bool CanCancel { get; }

    /// <summary>Whether Open output is enabled.</summary>
    bool CanOpenOutput { get; }

    /// <summary>Optional overall progress 0–1; null hides the bar.</summary>
    double? Progress { get; }

    /// <summary>Optional label beside the overall progress bar.</summary>
    string? ProgressLabel { get; }

    /// <summary>Optional per-item progress rows (e.g. chapters, packages, pipeline steps).</summary>
    IReadOnlyList<IJobStepProgress>? StepProgress { get; }

    /// <summary>Optional consumer payload.</summary>
    object? Tag { get; }
}

/// <summary>One step/item progress line under a job row.</summary>
public interface IJobStepProgress
{
    /// <summary>Display label (step label).</summary>
    string Label { get; }

    /// <summary>0–1 progress.</summary>
    double Progress { get; }

    /// <summary>Short status (e.g. <c>3/12</c>, <c>done</c>).</summary>
    string? StatusLabel { get; }
}

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
