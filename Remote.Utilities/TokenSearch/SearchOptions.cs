using System;

namespace Remote.Utilities.TokenSearch
{
    public class SearchOptions<T>
    {
        public virtual char[]? Delimiter { get; set; }

        public virtual int? MaxFilterTokenEntries { get; set; }

        public virtual PostProcessAlgorithm<T>? PostProcessAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets a filter used to pre-verify an entry.
        /// </summary>
        public virtual Func<T, bool>? PreProcessCheck { get; set; }

        public virtual SearchAlgorithm? SearchAlgorithm { get; set; }

        public virtual string[]? SearchProperties { get; set; }

        public virtual Comparison<SearchItem<T>>? SortAlgorithm { get; set; }

        // At what point does the match algorithm give up. A threshold of '0.0' requires a perfect match
        // (of both letters and location), a threshold of '1.0' would match anything.
        public virtual double? Threshold { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the result should contain just unique entries (based on search properties).
        /// </summary>
        public virtual bool Unique { get; set; }
    }
}