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

    /// <summary>Optional consumer payload.</summary>
    object? Tag { get; }
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
    public object? Tag { get; init; }
}
