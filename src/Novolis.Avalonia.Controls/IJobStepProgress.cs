namespace Novolis.Avalonia.Controls;

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
