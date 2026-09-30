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
