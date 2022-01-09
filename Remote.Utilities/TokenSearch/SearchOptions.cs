using System;

namespace Remote.Utilities.TokenSearch;

public sealed class SearchOptions<T>
    where T : notnull
{
    public char[]? Delimiter { get; set; }

    public int? MaxFilterTokenEntries { get; set; }

    public PostProcessAlgorithm<T>? PostProcessAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets a filter used to pre-verify an entry.
    /// </summary>
    public Func<T, bool>? PreProcessCheck { get; set; }

    public ScoringAlgorithm? ScoringAlgorithm { get; set; }

    public string[]? SearchProperties { get; set; }

    public Comparison<SearchItem<T>>? SortAlgorithm { get; set; }

    // At what point does the match algorithm give up. A threshold of '0.0' requires a perfect match
    // (of both letters and location), a threshold of '1.0' would match anything.
    public double? Threshold { get; set; }

    /// <summary>
    /// Gets or sets a value indicating if the result should contain just unique entries (based on search properties).
    /// </summary>
    public bool Unique { get; set; }
}
