using System.Collections.Generic;

namespace Remote.Utilities.TokenSearch
{
    public delegate IEnumerable<SearchItem<T>> PostProcessAlgorithm<T>(IEnumerable<SearchItem<T>> searchItems, int maxScore, double threshold, bool unique, string[]? searchProperties)
        where T : notnull;
}