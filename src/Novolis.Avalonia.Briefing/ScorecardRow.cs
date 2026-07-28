namespace Novolis.Avalonia.Briefing;

/// <summary>One scorecard row (filled when <see cref="Hits"/> &gt; 0).</summary>
public sealed class ScorecardRow
{
    /// <summary>Creates a scorecard row.</summary>
    public ScorecardRow(string kind, int hits, string hook, bool filled = true)
    {
        Kind = kind;
        Hits = hits;
        Hook = hook;
        Filled = filled && hits > 0;
    }

    /// <summary>Moment / category id.</summary>
    public string Kind { get; }

    /// <summary>How many times it fired.</summary>
    public int Hits { get; }

    /// <summary>Short player-facing hook.</summary>
    public string Hook { get; }

    /// <summary>Whether the moment is considered landed.</summary>
    public bool Filled { get; }
}
